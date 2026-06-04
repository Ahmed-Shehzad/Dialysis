namespace Dialysis.SmartConnect.TimeSync;

/// <summary>
/// Outcome of one <see cref="Hl7V2ClockSkewProbe.TryObserveAndCorrect"/> call. Carries
/// enough context for an audit event ("at 13:59:59 UTC we rewrote MSH-7 from 14:01:43
/// to 14:00:00 for source MachineA@FACILITY") so we never silently re-time a clinical
/// message.
/// </summary>
public sealed record ClockSkewCorrectionResult
{
    /// <summary>
    /// Outcome of one <see cref="Hl7V2ClockSkewProbe.TryObserveAndCorrect"/> call. Carries
    /// enough context for an audit event ("at 13:59:59 UTC we rewrote MSH-7 from 14:01:43
    /// to 14:00:00 for source MachineA@FACILITY") so we never silently re-time a clinical
    /// message.
    /// </summary>
    public ClockSkewCorrectionResult(string SourceId,
        DateTime OriginalMessageTimestampUtc,
        DateTime ObservedAtUtc,
        TimeSpan ObservedSkew,
        DateTime? CorrectedMessageTimestampUtc,
        bool WasCorrected,
        string? RejectionReason)
    {
        this.SourceId = SourceId;
        this.OriginalMessageTimestampUtc = OriginalMessageTimestampUtc;
        this.ObservedAtUtc = ObservedAtUtc;
        this.ObservedSkew = ObservedSkew;
        this.CorrectedMessageTimestampUtc = CorrectedMessageTimestampUtc;
        this.WasCorrected = WasCorrected;
        this.RejectionReason = RejectionReason;
    }
    public string SourceId { get; init; }
    public DateTime OriginalMessageTimestampUtc { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public TimeSpan ObservedSkew { get; init; }
    public DateTime? CorrectedMessageTimestampUtc { get; init; }
    public bool WasCorrected { get; init; }
    public string? RejectionReason { get; init; }
    public void Deconstruct(out string SourceId, out DateTime OriginalMessageTimestampUtc, out DateTime ObservedAtUtc, out TimeSpan ObservedSkew, out DateTime? CorrectedMessageTimestampUtc, out bool WasCorrected, out string? RejectionReason)
    {
        SourceId = this.SourceId;
        OriginalMessageTimestampUtc = this.OriginalMessageTimestampUtc;
        ObservedAtUtc = this.ObservedAtUtc;
        ObservedSkew = this.ObservedSkew;
        CorrectedMessageTimestampUtc = this.CorrectedMessageTimestampUtc;
        WasCorrected = this.WasCorrected;
        RejectionReason = this.RejectionReason;
    }
}
