namespace Transponder.OpenTelemetry;

/// <summary>
/// Configures Transponder OpenTelemetry instrumentation.
/// </summary>
public sealed class TransponderOpenTelemetryOptions
{
    public string ActivitySourceName { get; set; } = "Transponder";

    public string MeterName { get; set; } = "Transponder";

    public string? InstrumentationVersion { get; set; }

    public bool EnableTracing { get; set; } = true;

    public bool EnableMetrics { get; set; } = true;
}
