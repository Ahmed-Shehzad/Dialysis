using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Transponder.OpenTelemetry;

/// <summary>
/// Owns OpenTelemetry sources and metrics for Transponder.
/// </summary>
public sealed class TransponderOpenTelemetryInstrumentation : IDisposable
{
    public TransponderOpenTelemetryInstrumentation(TransponderOpenTelemetryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ActivitySourceName))
            throw new ArgumentException("ActivitySourceName must be provided.", nameof(options));

        if (string.IsNullOrWhiteSpace(options.MeterName))
            throw new ArgumentException("MeterName must be provided.", nameof(options));

        string version = options.InstrumentationVersion
                         ?? typeof(TransponderOpenTelemetryInstrumentation).Assembly
                             .GetName()
                             .Version?
                             .ToString()
                         ?? "unknown";

        ActivitySource = new ActivitySource(options.ActivitySourceName, version);
        Meter = new Meter(options.MeterName, version);

        SendCounter = Meter.CreateCounter<long>("transponder.messages.sent");
        PublishCounter = Meter.CreateCounter<long>("transponder.messages.published");
        ConsumeCounter = Meter.CreateCounter<long>("transponder.messages.consumed");
    }

    public ActivitySource ActivitySource { get; }

    public Meter Meter { get; }

    public Counter<long> SendCounter { get; }

    public Counter<long> PublishCounter { get; }

    public Counter<long> ConsumeCounter { get; }

    public void Dispose()
    {
        ActivitySource.Dispose();
        Meter.Dispose();
    }
}
