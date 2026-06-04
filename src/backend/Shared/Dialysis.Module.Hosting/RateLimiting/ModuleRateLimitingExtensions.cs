using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dialysis.Module.Hosting.RateLimiting;

/// <summary>
/// Token-bucket rate limiter applied to every module API. Sheds load at the API tier before
/// the durable bus / RMQ / Postgres tiers feel it; works with the other backpressure
/// surfaces documented in <c>docs/operations/load-and-capacity.md</c>.
/// </summary>
public static class ModuleRateLimitingExtensions
{
    /// <summary>Policy name the middleware applies — exposed so endpoints can opt out via <c>.DisableRateLimiting()</c>.</summary>
    public const string PolicyName = "module-default";

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the token-bucket rate limiter keyed on the authenticated subject (sub
        /// claim) and falling back to the caller's IP. Reads its budget from
        /// <c>&lt;Module&gt;:RateLimit</c> in configuration via <see cref="ModuleRateLimitOptions"/>.
        /// </summary>
        public IServiceCollection AddModuleRateLimiting(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.Configure<ModuleRateLimitOptions>(configuration.GetSection(ModuleRateLimitOptions.SectionName));

            services.AddRateLimiter(rl =>
            {
                rl.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                rl.OnRejected = static (ctx, ct) =>
                {
                    // Surface a Retry-After hint so well-behaved clients back off. The token-bucket
                    // replenishment rate gives a deterministic upper bound on how long to wait.
                    var opts = ctx.HttpContext.RequestServices
                        .GetRequiredService<IOptions<ModuleRateLimitOptions>>().Value;
                    var retryAfterSeconds = Math.Max(1, 60 / Math.Max(1, opts.TokensPerSecond / 60));
                    ctx.HttpContext.Response.Headers["Retry-After"] = retryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    return ValueTask.CompletedTask;
                };
                rl.AddPolicy(PolicyName, http =>
                {
                    var opts = http.RequestServices.GetRequiredService<IOptions<ModuleRateLimitOptions>>().Value;
                    if (!opts.Enabled)
                        return RateLimitPartition.GetNoLimiter("disabled");

                    var partitionKey = ResolvePartitionKey(http);
                    return RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey,
                        _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = opts.BurstCapacity,
                            TokensPerPeriod = opts.TokensPerSecond,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                            QueueLimit = opts.QueueLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            AutoReplenishment = true,
                        });
                });
            });

            return services;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>Activates the rate limiter and applies <see cref="PolicyName"/> to MVC + minimal-API endpoints.</summary>
        public WebApplication UseModuleRateLimiting()
        {
            ArgumentNullException.ThrowIfNull(app);
            var opts = app.Services.GetRequiredService<IOptions<ModuleRateLimitOptions>>().Value;
            if (!opts.Enabled)
                return app;
            app.UseRateLimiter();
            return app;
        }
    }

    private static string ResolvePartitionKey(HttpContext http)
    {
        // Authenticated callers — partition by subject so a high-traffic clinician doesn't
        // share a budget with the device fleet or with anonymous requests.
        var subject = http.User.FindFirst("sub")?.Value
            ?? http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(subject))
            return "sub:" + subject;
        // Unauthenticated — partition by IP. Picks up x-forwarded-for when behind the gateway.
        var ip = http.Connection.RemoteIpAddress?.ToString()
            ?? http.Request.Headers["X-Forwarded-For"].ToString();
        return "ip:" + (string.IsNullOrEmpty(ip) ? "unknown" : ip);
    }
}
