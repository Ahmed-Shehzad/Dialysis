namespace Dialysis.Treatment.Application.Abstractions;

public sealed record RecordAlarmFromThresholdBreachClientRequest(
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

/// <summary>
/// Client for creating alarms from threshold breaches (cross-context: Treatment → Alarm).
/// When AlarmApi:BaseUrl is not configured, implementations may no-op.
/// </summary>
public interface IAlarmApiClient
{
    Task<bool> RecordFromThresholdBreachAsync(
        RecordAlarmFromThresholdBreachClientRequest request,
        CancellationToken cancellationToken = default);
}
