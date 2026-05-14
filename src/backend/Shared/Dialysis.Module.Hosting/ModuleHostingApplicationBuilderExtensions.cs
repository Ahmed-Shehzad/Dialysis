using Dialysis.Module.Hosting.Authorization;
using Dialysis.Module.Hosting.Health;
using Dialysis.Module.Hosting.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dialysis.Module.Hosting;

public static class ModuleHostingApplicationBuilderExtensions
{
    /// <summary>
    /// Wires the standard module middleware pipeline: HSTS + security headers in Production,
    /// global exception handler (incl. problem-details), correlation id, authentication (when
    /// configured), authorization, and the live/ready health endpoints.
    /// Modules call this once before mapping their own endpoints/controllers.
    /// </summary>
    public static WebApplication UseModuleHost(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }
        app.UseSecurityHeaders();
        app.UseExceptionHandler();
        app.UseMiddleware<CorrelationIdMiddleware>();

        var auth = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ModuleAuthenticationOptions>>().Value;
        if (!string.IsNullOrWhiteSpace(auth.Authority))
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        app.MapModuleHealthEndpoints();
        return app;
    }

    /// <summary>Maps the canonical module health endpoints onto an arbitrary endpoint route builder (e.g. a versioned group).</summary>
    public static IEndpointRouteBuilder MapModuleHealth(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapModuleHealthEndpoints();
}
