using BuildingBlocks;

namespace Dialysis.Alarm.Application.Events;

/// <summary>
/// Integration event published when AlarmEscalationService determines escalation is needed (3+ active alarms in 5 min).
/// Raised by EscalationIncident aggregate; dispatched post-commit via Outbox. Consumable by nursing dashboard, FHIR DetectedIssue.
/// </summary>
public sealed record AlarmEscalationTriggeredEvent(
    string? DeviceId,
    string? SessionId,
    int ActiveAlarmCount,
    string Reason,
    string? TenantId) : IntegrationEvent(Ulid.NewUlid());
