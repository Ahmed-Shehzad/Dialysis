namespace Dialysis.SmartConnect.TimeSync;

/// <summary>
/// Per-source clock-skew snapshot. <see cref="SourceId"/> is whatever the operator uses
/// to identify the upstream — for HL7v2 it's typically <c>MSH-3</c> (sending application)
/// or the machine serial parsed off <c>MSH-3.2</c>. Skew is computed as
/// <c>(serverNow - messageTs)</c>, so a positive value means the upstream's clock is
/// behind ours.
/// </summary>
public sealed record ClockSkewObservation
{
    /// <summary>
    /// Per-source clock-skew snapshot. <see cref="SourceId"/> is whatever the operator uses
    /// to identify the upstream — for HL7v2 it's typically <c>MSH-3</c> (sending application)
    /// or the machine serial parsed off <c>MSH-3.2</c>. Skew is computed as
    /// <c>(serverNow - messageTs)</c>, so a positive value means the upstream's clock is
    /// behind ours.
    /// </summary>
    public ClockSkewObservation(string SourceId,
        DateTime MessageTimestampUtc,
        DateTime ObservedAtUtc,
        TimeSpan Skew)
    {
        this.SourceId = SourceId;
        this.MessageTimestampUtc = MessageTimestampUtc;
        this.ObservedAtUtc = ObservedAtUtc;
        this.Skew = Skew;
    }
    public string SourceId { get; init; }
    public DateTime MessageTimestampUtc { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public TimeSpan Skew { get; init; }
    public void Deconstruct(out string SourceId, out DateTime MessageTimestampUtc, out DateTime ObservedAtUtc, out TimeSpan Skew)
    {
        SourceId = this.SourceId;
        MessageTimestampUtc = this.MessageTimestampUtc;
        ObservedAtUtc = this.ObservedAtUtc;
        Skew = this.Skew;
    }
}

/// <summary>
/// Aggregated skew status for one upstream. The IG (§2 Time Synchronization) requires
/// dialysis machines to participate in IHE Consistent Time / NTP; this signal lets
/// operators catch drift before it corrupts treatment timestamps.
/// </summary>
public sealed record ClockSkewStatus
{
    /// <summary>
    /// Aggregated skew status for one upstream. The IG (§2 Time Synchronization) requires
    /// dialysis machines to participate in IHE Consistent Time / NTP; this signal lets
    /// operators catch drift before it corrupts treatment timestamps.
    /// </summary>
    public ClockSkewStatus(string SourceId,
        DateTime LastObservedAtUtc,
        TimeSpan LastSkew,
        TimeSpan MaxAbsSkewWindow,
        int ObservationCount)
    {
        this.SourceId = SourceId;
        this.LastObservedAtUtc = LastObservedAtUtc;
        this.LastSkew = LastSkew;
        this.MaxAbsSkewWindow = MaxAbsSkewWindow;
        this.ObservationCount = ObservationCount;
    }

    /// <summary>
    /// Tone classification matching the SPA badge palette:
    /// <c>"healthy"</c> if |skew| &lt; 1s (CT tolerance per RFC 1305 / IHE CT),
    /// <c>"warning"</c> if &lt; 30s, <c>"alert"</c> otherwise.
    /// </summary>
    public string Severity => Math.Abs(LastSkew.TotalSeconds) switch
    {
        < 1.0 => "healthy",
        < 30.0 => "warning",
        _ => "alert",
    };

    public string SourceId { get; init; }
    public DateTime LastObservedAtUtc { get; init; }
    public TimeSpan LastSkew { get; init; }
    public TimeSpan MaxAbsSkewWindow { get; init; }
    public int ObservationCount { get; init; }
    public void Deconstruct(out string SourceId, out DateTime LastObservedAtUtc, out TimeSpan LastSkew, out TimeSpan MaxAbsSkewWindow, out int ObservationCount)
    {
        SourceId = this.SourceId;
        LastObservedAtUtc = this.LastObservedAtUtc;
        LastSkew = this.LastSkew;
        MaxAbsSkewWindow = this.MaxAbsSkewWindow;
        ObservationCount = this.ObservationCount;
    }
}
