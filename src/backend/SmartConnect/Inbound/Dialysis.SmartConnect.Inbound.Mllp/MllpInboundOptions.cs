namespace Dialysis.SmartConnect.Inbound.Mllp;

/// <summary>
/// TCP listener options for MLLP-style inbound. Bind section <c>SmartConnect:Mllp</c>.
/// </summary>
public sealed class MllpInboundOptions
{
    /// <summary>Listen address (default all interfaces).</summary>
    public string ListenAddress { get; set; } = "0.0.0.0";

    public int ListenPort { get; set; } = 2575;

    /// <summary>All messages are dispatched to this integration flow.</summary>
    public Guid DefaultFlowId { get; set; }

    /// <summary>Maximum HL7 payload bytes inside one frame (default 8 MiB).</summary>
    public int MaxMessageBytes { get; set; } = 8 * 1024 * 1024;
}
