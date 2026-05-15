using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dialysis.ServiceDefaults;

/// <summary>
/// Aspire-style cross-cutting wire-up for module hosts. OpenTelemetry, health checks and
/// problem-details are already configured by <c>Dialysis.Module.Hosting</c>; this layer only
/// adds the pieces the AppHost orchestration depends on — service discovery and the standard
/// HTTP client resilience handler — so that <c>WithReference</c> lookups (e.g. <c>http://his-api</c>)
/// resolve at runtime and outbound HTTP calls get retries / circuit-breaker / timeouts by default.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers service discovery and applies standard resilience + discovery to every named
    /// <see cref="System.Net.Http.HttpClient"/>. Safe to call alongside <c>AddModuleHost</c>;
    /// it does not touch OTel, health checks, or authentication.
    /// </summary>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// Maps the lightweight liveness probe (<c>/alive</c>) used by the Aspire dashboard.
    /// The richer per-module readiness probe is mapped by <c>UseModuleHost()</c>; this only
    /// adds the "process is up" probe that has no DI / DB dependencies.
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Environment.IsDevelopment())
        {
            app.MapGet("/alive", () => Results.Ok(new { status = "alive" }))
                .AllowAnonymous()
                .WithName("aspire-alive");
        }

        return app;
    }
}
