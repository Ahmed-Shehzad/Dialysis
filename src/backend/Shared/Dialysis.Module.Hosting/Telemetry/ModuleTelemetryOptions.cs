namespace Dialysis.Module.Hosting.Telemetry;

public sealed class ModuleTelemetryOptions
{
    /// <summary>OTel <c>service.name</c> attribute. Defaults to the module slug if unset.</summary>
    public string? ServiceName { get; set; }

    /// <summary>Optional OTel <c>service.version</c> attribute.</summary>
    public string? ServiceVersion { get; set; }

    /// <summary>OTLP endpoint (e.g. <c>http://otel-collector:4317</c>). When unset, the console exporter is used.</summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>Activity sources contributed by the module (e.g. domain spans, scheduled work).</summary>
    public List<string> AdditionalActivitySources { get; } = new();

    /// <summary>Meter names contributed by the module.</summary>
    public List<string> AdditionalMeters { get; } = new();
}
