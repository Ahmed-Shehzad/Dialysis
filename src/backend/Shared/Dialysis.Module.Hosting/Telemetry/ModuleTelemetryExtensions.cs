using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Dialysis.Module.Hosting.Telemetry;

public static class ModuleTelemetryExtensions
{
    /// <summary>Default activity-source name used by the Transponder building block for publish/consume spans.</summary>
    public const string TransponderActivitySource = "Dialysis.BuildingBlocks.Transponder";

    /// <summary>
    /// Registers OpenTelemetry tracing and metrics with sensible defaults for an ASP.NET module host:
    /// ASP.NET Core + HttpClient + EF Core + Npgsql instrumentation, the module's own activity
    /// sources/meters, the Transponder activity source, and either the OTLP exporter (when
    /// <see cref="ModuleTelemetryOptions.OtlpEndpoint"/> is set) or the console exporter as fallback.
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
        var hasOtlp = !string.IsNullOrWhiteSpace(options.OtlpEndpoint);

        services.AddOpenTelemetry()
            .ConfigureResource(r =>
            {
                r.AddService(serviceName, serviceVersion: options.ServiceVersion);
                r.AddAttributes(
                [
                    new("dialysis.module", moduleSlug)
                ]);
            })
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation();
                t.AddHttpClientInstrumentation();
                t.AddEntityFrameworkCoreInstrumentation();
                t.AddNpgsql();
                t.AddSource(TransponderActivitySource);
                foreach (var source in options.AdditionalActivitySources)
                    t.AddSource(source);

                if (hasOtlp)
                    t.AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint!));
                else
                    t.AddConsoleExporter();
            })
            .WithMetrics(m =>
            {
                m.AddAspNetCoreInstrumentation();
                m.AddHttpClientInstrumentation();
                foreach (var meter in options.AdditionalMeters)
                    m.AddMeter(meter);

                if (hasOtlp)
                    m.AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint!));
                else
                    m.AddConsoleExporter();
            });

        return services;
    }
}
