using BuildingBlocks;

namespace Dialysis.Alarm.Application.Events;

/// <summary>
/// Integration event published when AlarmEscalationService determines escalation is needed (3+ active alarms in 5 min).
/// Dispatched post-commit via IIntegrationEventBuffer. Consumable by nursing dashboard, FHIR DetectedIssue.
/// </summary>
public sealed record AlarmEscalationTriggeredEvent(
    string? DeviceId,
    string? SessionId,
    int ActiveAlarmCount,
    string Reason,
    string? TenantId) : IntegrationEvent(Ulid.NewUlid());
