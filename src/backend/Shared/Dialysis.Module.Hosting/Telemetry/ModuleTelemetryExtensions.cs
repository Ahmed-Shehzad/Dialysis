using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Dialysis.Module.Hosting.Telemetry;

public static class ModuleTelemetryExtensions
{
    /// <summary>
    /// Registers OpenTelemetry tracing and metrics with sensible defaults for an ASP.NET module host:
    /// ASP.NET Core + HttpClient instrumentation, the module's own activity sources/meters, and a console exporter.
    /// Hosts that need OTLP can layer <c>AddOtlpExporter</c> on the same <see cref="OpenTelemetryBuilder"/>
    /// by referencing <c>OpenTelemetry.Exporter.OpenTelemetryProtocol</c> in their own host project.
    /// </summary>
    public static IServiceCollection AddModuleTelemetry(
        this IServiceCollection services,
        string moduleSlug,
        Action<ModuleTelemetryOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleSlug);

        var options = new ModuleTelemetryOptions();
        configure?.Invoke(options);
        var serviceName = string.IsNullOrWhiteSpace(options.ServiceName) ? $"Dialysis.{moduleSlug}" : options.ServiceName;

        services.AddOpenTelemetry()
            .ConfigureResource(r =>
            {
                r.AddService(serviceName, serviceVersion: options.ServiceVersion);
                r.AddAttributes(new KeyValuePair<string, object>[]
                {
                    new("dialysis.module", moduleSlug),
                });
            })
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation();
                t.AddHttpClientInstrumentation();
                foreach (var source in options.AdditionalActivitySources)
                    t.AddSource(source);
                t.AddConsoleExporter();
            })
            .WithMetrics(m =>
            {
                m.AddAspNetCoreInstrumentation();
                m.AddHttpClientInstrumentation();
                foreach (var meter in options.AdditionalMeters)
                    m.AddMeter(meter);
                m.AddConsoleExporter();
            });

        return services;
    }
}
