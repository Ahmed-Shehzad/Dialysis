using BuildingBlocks;

namespace Dialysis.Treatment.Application.Events;

/// <summary>
/// Integration event published when a clinical threshold breach is detected. Raised by TreatmentSession
/// aggregate; dispatched post-commit via Outbox. Consumable by Alarm context (create DetectedIssue) or analytics.
/// </summary>
public sealed record ThresholdBreachDetectedIntegrationEvent(
    Ulid TreatmentSessionId,
    string SessionId,
    string? DeviceId,
    Ulid ObservationId,
    string Code,
    string BreachType,
    double ObservedValue,
    double ThresholdValue,
    string Direction,
    string? TenantId) : IntegrationEvent(TreatmentSessionId);
