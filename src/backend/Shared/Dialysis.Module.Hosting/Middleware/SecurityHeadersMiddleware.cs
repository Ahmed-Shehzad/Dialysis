using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Dialysis.Module.Hosting.Middleware;

/// <summary>
/// Applies the security-header baseline expected by C5 / OWASP ASVS Level 2 to every response:
/// strict-transport-security (handled by <c>UseHsts()</c> in Production), nosniff, referrer policy,
/// frame deny, permissions policy, and a conservative content-security-policy default for API
/// responses (no inline script, no remote origins).
/// Modules call <c>app.UseSecurityHeaders()</c> early in the pipeline.
/// </summary>
public static class SecurityHeadersMiddleware
{
    extension(IApplicationBuilder app)
    {
        public IApplicationBuilder UseSecurityHeaders()
        {
            ArgumentNullException.ThrowIfNull(app);
            return app.Use(static async (context, next) =>
            {
                var headers = context.Response.Headers;
                headers.Append("X-Content-Type-Options", "nosniff");
                headers.Append("Referrer-Policy", "no-referrer");
                headers.Append("X-Frame-Options", "DENY");
                headers.Append(
                    "Permissions-Policy",
                    "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
                // API responses get a locked-down CSP (no inline, no remote origins). The Hangfire
                // dashboard is an HTML admin UI served from this same host: it links its own /hangfire/*
                // CSS + JS and uses inline <style>/<script> blocks, so the API's default-src 'none'
                // renders it completely unstyled. Scope a self + unsafe-inline policy to that path only.
                headers.Append(
                    "Content-Security-Policy",
                    context.Request.Path.StartsWithSegments("/hangfire")
                        ? "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; frame-ancestors 'none'"
                        : "default-src 'none'; frame-ancestors 'none'");
                headers.Append("Cross-Origin-Opener-Policy", "same-origin");
                headers.Append("Cross-Origin-Resource-Policy", "same-site");
                await next.Invoke().ConfigureAwait(false);
            });
        }
    }
}
