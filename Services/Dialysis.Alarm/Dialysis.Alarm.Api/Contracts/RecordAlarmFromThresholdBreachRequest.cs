using System.Text.Json.Serialization;

namespace Dialysis.Alarm.Api.Contracts;

/// <summary>
/// Request body for POST /api/alarms/from-threshold-breach (internal cross-context).
/// </summary>
public sealed record RecordAlarmFromThresholdBreachRequest(
    [property: JsonRequired] string SessionId,
    string? DeviceId,
    [property: JsonRequired] string BreachType,
    [property: JsonRequired] string Code,
    [property: JsonRequired] double ObservedValue,
    [property: JsonRequired] double ThresholdValue,
    [property: JsonRequired] string Direction,
    [property: JsonRequired] string TreatmentSessionId,
    [property: JsonRequired] string ObservationId,
    string? TenantId);
