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

    /// <summary>
    /// Slice J3: clock-skew correction policy applied to every framed message. Defaults
    /// to <c>ReportOnly</c> (probe runs, monitor accumulates, MSH-7 left untouched).
    /// Configure <c>SmartConnect:Mllp:ClockSkew</c> to opt into normalisation.
    /// </summary>
    public MllpClockSkewOptions ClockSkew { get; set; } = new();
}

/// <summary>
/// Per-host clock-skew policy options. Per-flow / per-source overrides would require a
/// source-connector schema change; this slice deliberately keeps the policy at the host
/// level so operators can dial in correction without per-flow code-gen churn.
/// </summary>
public sealed class MllpClockSkewOptions
{
    /// <summary>One of <c>ReportOnly</c> / <c>Normalize</c> (case-insensitive). Default
    /// <c>ReportOnly</c> preserves the slice J2 behaviour byte-for-byte.</summary>
    public string Mode { get; set; } = "ReportOnly";

    /// <summary>Lower bound (seconds) of absolute skew that triggers a correction. Only
    /// honoured when <see cref="Mode"/> is <c>Normalize</c>. Default 1 s — sub-second
    /// drift is below IHE Consistent Time tolerance.</summary>
    public double CorrectAboveAbsSkewSeconds { get; set; } = 1.0;

    /// <summary>Upper bound (seconds) of absolute skew the corrector will touch. Beyond
    /// this we refuse to retime — a 24h-ahead message is much more likely to be a
    /// misconfiguration than a real reading. Default 3600 s (1 h).</summary>
    public double MaxAllowedAbsJumpSeconds { get; set; } = 3600.0;
}
