using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Namespace deliberately omits the `Hangfire` segment so `using Hangfire;` binds the global namespace.
namespace Dialysis.BuildingBlocks.Transponder.Hosting;

/// <summary>
/// Mounts the Hangfire dashboard for a host. The dashboard lives at <c>/hangfire</c> on the host's own
/// endpoint (the AppHost surfaces a one-click link per resource); when reached through the edge Gateway
/// at <c>/{ctx}/hangfire</c>, an <c>X-Forwarded-Prefix</c> shim rewrites the request path base so the
/// dashboard's own links/assets keep the context prefix.
/// </summary>
public static class HangfireDashboardExtensions
{
    extension(WebApplication app)
    {
        /// <summary>
        /// Adds the Hangfire dashboard at <c>/hangfire</c> when Hangfire is configured (no-op otherwise,
        /// e.g. tests). Authorization is open in Development and authenticated-only elsewhere.
        /// </summary>
        public WebApplication UseModuleHangfireDashboard(string title)
        {
            ArgumentNullException.ThrowIfNull(app);

            // No Hangfire storage configured (ConnectionStrings:Hangfire absent) → nothing to surface.
            if (app.Services.GetService<JobStorage>() is null)
            {
                return app;
            }

            // Honour X-Forwarded-Prefix only for the dashboard branch, so requests proxied by the Gateway
            // at /{ctx}/hangfire render context-prefixed links without affecting other endpoints.
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/hangfire"))
                {
                    var prefix = context.Request.Headers["X-Forwarded-Prefix"].ToString();
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        context.Request.PathBase = new PathString(prefix);
                    }
                }

                await next().ConfigureAwait(false);
            });

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                DashboardTitle = $"Hangfire · {title}",
                Authorization = [new ModuleHangfireDashboardAuthorizationFilter(app.Environment.IsDevelopment())],
                IgnoreAntiforgeryToken = true,
            });

            return app;
        }
    }
}

/// <summary>
/// Dashboard authorization: open in Development (local dev convenience behind the Gateway/BFF), and
/// restricted to an authenticated principal in every other environment.
/// </summary>
internal sealed class ModuleHangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly bool _allowAnonymous;

    public ModuleHangfireDashboardAuthorizationFilter(bool allowAnonymous) => _allowAnonymous = allowAnonymous;

    public bool Authorize(DashboardContext context)
    {
        if (_allowAnonymous)
        {
            return true;
        }

        var httpContext = context.GetHttpContext();
        return httpContext.User?.Identity?.IsAuthenticated == true;
    }
}
