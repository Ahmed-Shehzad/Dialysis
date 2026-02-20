using Refit;

namespace Dialysis.Treatment.Infrastructure.AlarmApi;

internal interface IAlarmApi
{
    [Post("/api/alarms/from-threshold-breach")]
    Task<ApiResponse<RecordAlarmFromThresholdBreachApiResponse>> RecordFromThresholdBreachAsync(
        [Body] RecordAlarmFromThresholdBreachApiRequest request,
        [Header("X-Tenant-Id")] string? tenantId,
        CancellationToken cancellationToken = default);
}

internal sealed record RecordAlarmFromThresholdBreachApiRequest(
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

internal sealed record RecordAlarmFromThresholdBreachApiResponse(string AlarmId);
