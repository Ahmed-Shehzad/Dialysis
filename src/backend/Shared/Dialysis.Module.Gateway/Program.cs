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

// Security posture: never let an empty AllowedOrigins config silently fall through to a
// permissive policy in production. The dev fallback below only fires in Development; any
// other environment must declare its origins via Gateway:Cors:AllowedOrigins or the
// gateway won't start. This closes the "developer forgets to tighten CORS before deploy"
// trap before it can happen — there is no codepath where a non-Development environment
// ends up with an unconfigured allowlist.
if (allowedOrigins.Length == 0 && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "Gateway:Cors:AllowedOrigins must be set in non-Development environments. " +
        $"Current environment: {builder.Environment.EnvironmentName}.");
}

builder.Services.AddCors(o => o.AddPolicy(corsPolicyName, p =>
{
    if (allowedOrigins.Length == 0)
    {
        // Dev-only fallback (guarded above). Allow the Vite dev server (when the SPA is
        // opened directly during local debugging) AND the gateway origin itself (the
        // canonical browser entry point — browsers send `Origin: http://localhost:9090`
        // on module/fetch requests even though the destination is same-origin, so
        // without it CORS middleware logs a rejection per asset request).
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
                    // SignalR hubs and the FHIR Subscription SSE/WebSocket streams cannot send
                    // an Authorization header from the browser (EventSource / WS), so they pass
                    // the bearer as an ?access_token= query parameter instead.
                    var isStreamingPath =
                        path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase) ||
                        (path.HasValue &&
                            (path.Value.Contains("/subscription/sse", StringComparison.OrdinalIgnoreCase) ||
                             path.Value.Contains("/subscription/websocket", StringComparison.OrdinalIgnoreCase)));
                    if (!string.IsNullOrEmpty(accessToken) && isStreamingPath)
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

var isDevelopment = app.Environment.IsDevelopment();

// Security headers — full baseline matching what module hosts apply via
// SecurityHeadersMiddleware. The gateway is the public entry point so this is the layer
// browsers actually see for every response (including the SPA catch-all and the gateway's
// own /health, /_gateway endpoints), so the headers have to be applied here too — not
// only when proxying through to a module that runs the middleware.
//
// Strict-Transport-Security stays conditional on the request not arriving over the
// localhost loopback: HSTS over HTTP-localhost is rejected by browsers anyway, and adding
// it in the dev smoke loop makes the loopback unusable from a real browser session that
// previously visited the host over HTTPS.
app.Use(async (ctx, next) =>
{
    var headers = ctx.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "no-referrer";
    headers["X-Frame-Options"] = "DENY";
    headers["Permissions-Policy"] =
        "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
    // Catch-all routes the SPA HTML; CSP must allow the bundle to load. APIs return JSON
    // and don't need a tighter CSP at this layer — module-side `UseSecurityHeaders()`
    // applies `default-src 'none'` for JSON responses.
    //
    // Production keeps a strict `script-src 'self'` (the SPA ships no inline scripts:
    // theme bootstrap is the same-origin /theme-init.js). Development relaxes script-src to
    // allow Vite's React Fast-Refresh preamble + HMR, which the plugin injects inline and
    // which requires `eval`; `connect-src` is widened for the HMR websocket. Dev-only.
    headers["Content-Security-Policy"] = isDevelopment
        ? "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self' ws: wss:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'"
        : "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    headers["Cross-Origin-Opener-Policy"] = "same-origin";
    headers["Cross-Origin-Resource-Policy"] = "same-site";
    if (!ctx.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
    {
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
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
