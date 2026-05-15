using System.Threading.RateLimiting;
using Dialysis.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

const string corsPolicyName = "SpaCors";
const string anonymousRateLimitPolicy = "anonymous";
const string authenticatedRateLimitPolicy = "authenticated";

var gatewayOptions = builder.Configuration.GetSection("Gateway");
var authority = gatewayOptions["Authority"]?.Trim();
var audience = gatewayOptions["Audience"]?.Trim();
var requireAuth = gatewayOptions.GetValue("RequireAuthentication", !builder.Environment.IsDevelopment());

var allowedOrigins = gatewayOptions.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(o => o.AddPolicy(corsPolicyName, p =>
{
    if (allowedOrigins.Length == 0)
    {
        // Dev fallback: allow the Vite dev server (when the SPA is opened directly during
        // local debugging) AND the gateway origin itself (the canonical browser entry point —
        // browsers send `Origin: http://localhost:9090` on module/fetch requests even though
        // the destination is same-origin, so without it CORS middleware logs a rejection per
        // asset request).
        p.WithOrigins(
                "http://localhost:5173",
                "http://localhost:4173",
                "http://localhost:9090")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    }
    else
    {
        p.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    }
}));

if (!string.IsNullOrEmpty(authority))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.Authority = authority;
            o.RequireHttpsMetadata = Uri.TryCreate(authority, UriKind.Absolute, out var u)
                && string.Equals(u.Scheme, "https", StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(audience))
            {
                o.Audience = audience;
            }
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = !string.IsNullOrEmpty(audience),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
            };
            // SignalR/WebSocket clients send token via query string when upgrading; honor it.
            o.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var accessToken = ctx.Request.Query["access_token"];
                    var path = ctx.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
            };
        });
}
else if (requireAuth)
{
    throw new InvalidOperationException(
        "Gateway:Authority is required when Gateway:RequireAuthentication=true (Production). " +
        "Set it to your Keycloak realm issuer (e.g. http://keycloak:8080/realms/dialysis).");
}

builder.Services.AddAuthorization(o =>
{
    // YARP route config selects per-route policy via `AuthorizationPolicy`. "anonymous" and
    // "default" are reserved policy names handled by YARP's built-in resolver, so we only
    // register the custom "authenticated" policy here — registering "anonymous" would collide.
    o.AddPolicy("authenticated", p => p.RequireAuthenticatedUser());
    o.FallbackPolicy = null;
});

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(anonymousRateLimitPolicy, ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
    o.AddPolicy(authenticatedRateLimitPolicy, ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.User.Identity?.Name ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 600,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 16,
                AutoReplenishment = true,
            }));
});

builder.Services.AddHealthChecks();

// YARP forwards Authorization headers by default; per-route transforms live in appsettings.json.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Security headers — minimal, gateway-side. Module hosts add their own deeper headers.
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    if (!ctx.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }
    await next().ConfigureAwait(false);
});

app.UseCors(corsPolicyName);
app.UseWebSockets();
app.UseRateLimiter();

if (!string.IsNullOrEmpty(authority))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

// Moved off "/" so the YARP catch-all route can forward "/" to the SPA (Vite dev server in
// dev, static files in prod). The gateway-info JSON is still available for debugging at the
// dedicated path below.
app.MapGet("/_gateway", () => Results.Ok(new
{
    gateway = "dialysis",
    routes = new[] { "/identity", "/his", "/smartconnect", "/ehr", "/pdms", "/hie", "/fhir", "/hubs" },
})).AllowAnonymous();

app.MapReverseProxy(pipeline =>
{
    pipeline.UseSessionAffinity();
    pipeline.UseLoadBalancing();
    pipeline.UsePassiveHealthChecks();
}).RequireRateLimiting(string.IsNullOrEmpty(authority) ? anonymousRateLimitPolicy : authenticatedRateLimitPolicy);

await app.RunAsync().ConfigureAwait(false);
