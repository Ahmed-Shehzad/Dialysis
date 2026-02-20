namespace Dialysis.Alarm.Api.Contracts;

/// <summary>
/// Request body for POST /api/alarms/from-threshold-breach (internal cross-context).
/// </summary>
public sealed record RecordAlarmFromThresholdBreachRequest(
    string SessionId,
    string? DeviceId,
    string BreachType,
    string Code,
    double ObservedValue,
    double ThresholdValue,
    string Direction,
    string TreatmentSessionId,
    string ObservationId,
    string? TenantId);
