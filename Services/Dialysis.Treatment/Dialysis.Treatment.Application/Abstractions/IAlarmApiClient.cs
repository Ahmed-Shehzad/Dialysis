namespace Dialysis.Treatment.Application.Abstractions;

/// <summary>
/// Client for creating alarms from threshold breaches (cross-context: Treatment → Alarm).
/// When AlarmApi:BaseUrl is not configured, implementations may no-op.
/// </summary>
public interface IAlarmApiClient
{
    Task<bool> RecordFromThresholdBreachAsync(
        string sessionId,
        string? deviceId,
        string breachType,
        string code,
        double observedValue,
        double thresholdValue,
        string direction,
        string treatmentSessionId,
        string observationId,
        string? tenantId,
        CancellationToken cancellationToken = default);
}
