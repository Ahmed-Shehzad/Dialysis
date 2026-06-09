using Dialysis.BuildingBlocks.Transponder.Hosting;
using Dialysis.Module.Hosting.Authorization;
using Dialysis.Module.Hosting.Health;
using Dialysis.Module.Hosting.Middleware;
using Dialysis.Module.Hosting.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dialysis.Module.Hosting;

public static class ModuleHostingApplicationBuilderExtensions
{
    extension(WebApplication app)
    {
        /// <summary>
        /// Wires the standard module middleware pipeline: HSTS + security headers in Production,
        /// global exception handler (incl. problem-details), correlation id, authentication (when
        /// configured), authorization, and the live/ready health endpoints.
        /// Modules call this once before mapping their own endpoints/controllers.
        /// </summary>
        public WebApplication UseModuleHost()
        {
            ArgumentNullException.ThrowIfNull(app);

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }
            app.UseSecurityHeaders();
            app.UseExceptionHandler();
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseModuleRateLimiting();

            var auth = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ModuleAuthenticationOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(auth.Authority))
            {
                app.UseAuthentication();
                app.UseAuthorization();
            }

            // Hangfire dashboard at /hangfire (no-op unless Hangfire is configured for this host).
            app.UseModuleHangfireDashboard(app.Environment.ApplicationName);

            app.MapModuleHealthEndpoints();
            return app;
        }
    }

    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>Maps the canonical module health endpoints onto an arbitrary endpoint route builder (e.g. a versioned group).</summary>
        public IEndpointRouteBuilder MapModuleHealth() =>
            endpoints.MapModuleHealthEndpoints();
    }
}
