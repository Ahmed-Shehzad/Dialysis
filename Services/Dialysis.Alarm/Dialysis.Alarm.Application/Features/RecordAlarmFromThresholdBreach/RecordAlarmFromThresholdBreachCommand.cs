using Intercessor.Abstractions;

namespace Dialysis.Alarm.Application.Features.RecordAlarmFromThresholdBreach;

/// <summary>
/// Creates an alarm from a clinical threshold breach (e.g. from Treatment context).
/// Used when Treatment detects hypotension, tachycardia, etc. and needs to record a DetectedIssue.
/// </summary>
public sealed record RecordAlarmFromThresholdBreachCommand(
    string SessionId,
    string? DeviceId,
    string BreachType,
    string Code,
    double ObservedValue,
    double ThresholdValue,
    string Direction,
    Ulid TreatmentSessionId,
    Ulid ObservationId,
    string? TenantId) : ICommand<RecordAlarmFromThresholdBreachResponse>;
