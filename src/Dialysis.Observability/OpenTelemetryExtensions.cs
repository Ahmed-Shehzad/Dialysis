using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Dialysis.Observability;

/// <summary>
/// OpenTelemetry configuration for Dialysis services.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tracing and metrics.
    /// Configure OTLP exporter via OpenTelemetry:OtlpEndpoint or OTEL_EXPORTER_OTLP_ENDPOINT.
    /// When OtlpEndpoint is empty, exports to http://localhost:4317 (Jaeger/Collector).
    /// </summary>
    public static IServiceCollection AddDialysisOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

        // Skip when no OTLP endpoint configured (e.g. local dev without collector)
        if (string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            return services;
        }

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
            });

        return services;
    }
}
