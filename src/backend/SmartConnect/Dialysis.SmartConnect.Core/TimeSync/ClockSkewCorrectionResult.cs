namespace Dialysis.SmartConnect.TimeSync;

/// <summary>
/// Outcome of one <see cref="Hl7V2ClockSkewProbe.TryObserveAndCorrect"/> call. Carries
/// enough context for an audit event ("at 13:59:59 UTC we rewrote MSH-7 from 14:01:43
/// to 14:00:00 for source MachineA@FACILITY") so we never silently re-time a clinical
/// message.
/// </summary>
public sealed record ClockSkewCorrectionResult(
    string SourceId,
    DateTime OriginalMessageTimestampUtc,
    DateTime ObservedAtUtc,
    TimeSpan ObservedSkew,
    DateTime? CorrectedMessageTimestampUtc,
    bool WasCorrected,
    string? RejectionReason);
