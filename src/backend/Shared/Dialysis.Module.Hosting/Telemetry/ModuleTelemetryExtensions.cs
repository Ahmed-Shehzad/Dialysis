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

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers OpenTelemetry tracing and metrics with sensible defaults for an ASP.NET module host:
        /// ASP.NET Core + HttpClient + EF Core + Npgsql instrumentation, the module's own activity
        /// sources/meters, the Transponder activity source, and either the OTLP exporter (when
        /// <see cref="ModuleTelemetryOptions.OtlpEndpoint"/> is set) or the console exporter as fallback.
        /// </summary>
        public IServiceCollection AddModuleTelemetry(
            string moduleSlug,
            Action<ModuleTelemetryOptions>? configure = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(moduleSlug);

            var options = new ModuleTelemetryOptions();
            configure?.Invoke(options);
            var serviceName = string.IsNullOrWhiteSpace(options.ServiceName) ? $"Dialysis.{moduleSlug}" : options.ServiceName;
            var otlpEndpoint = !string.IsNullOrWhiteSpace(options.OtlpEndpoint)
                ? options.OtlpEndpoint
                : Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
            var hasOtlp = !string.IsNullOrWhiteSpace(otlpEndpoint);

            // Region awareness — set via DIALYSIS_REGION env var (rendered into the
            // module Deployment by the Aspire k8s publisher / per-env compose .env).
            // Default to "local" when unset so dev traces are self-identifying. Region
            // tag flows onto every span + metric + log as the OTel `cloud.region`
            // resource attribute, giving the prod dashboards a per-region slice for
            // free in active-passive deployments.
            var region = Environment.GetEnvironmentVariable("DIALYSIS_REGION");
            if (string.IsNullOrWhiteSpace(region))
                region = "local";

            services.AddOpenTelemetry()
                .ConfigureResource(r =>
                {
                    r.AddService(serviceName, serviceVersion: options.ServiceVersion);
                    r.AddAttributes(
                    [
                        new KeyValuePair<string, object>("dialysis.module", moduleSlug),
                        new KeyValuePair<string, object>("cloud.region", region),
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
                        t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint!));
                    else
                        t.AddConsoleExporter();
                })
                .WithMetrics(m =>
                {
                    m.AddAspNetCoreInstrumentation();
                    m.AddHttpClientInstrumentation();
                    // Transponder outbox relay metrics (published/failed counters + oldest-pending-age
                    // gauge) — registered centrally so every module that flips EnableOutboxRelay is
                    // observable without per-host wiring; hosts without the relay just emit nothing.
                    // String literal: Module.Hosting must not reference Transponder persistence — the
                    // name is the stable contract (TransponderOutboxMetrics.MeterName).
                    m.AddMeter("Dialysis.Transponder.Outbox");
                    foreach (var meter in options.AdditionalMeters)
                        m.AddMeter(meter);

                    if (hasOtlp)
                        m.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint!));
                    else
                        m.AddConsoleExporter();
                });

            return services;
        }
    }
}
