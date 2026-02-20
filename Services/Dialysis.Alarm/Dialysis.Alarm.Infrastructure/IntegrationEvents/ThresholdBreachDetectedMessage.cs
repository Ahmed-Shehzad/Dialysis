namespace Dialysis.Alarm.Infrastructure.IntegrationEvents;

/// <summary>
/// DTO for deserializing ThresholdBreachDetectedIntegrationEvent from ASB.
/// Mirrors Treatment's event shape; Alarm does not reference Treatment.Application.
/// </summary>
internal sealed record ThresholdBreachDetectedMessage(
    string TreatmentSessionId,
    string SessionId,
    string? DeviceId,
    string ObservationId,
    string Code,
    string BreachType,
    double ObservedValue,
    double ThresholdValue,
    string Direction,
    string? TenantId);
