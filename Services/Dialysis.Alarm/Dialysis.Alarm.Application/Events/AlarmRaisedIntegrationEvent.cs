using BuildingBlocks;

using Dialysis.Alarm.Application.Domain.ValueObjects;

namespace Dialysis.Alarm.Application.Events;

/// <summary>
/// Integration event published when an alarm is raised.
/// Consumable by downstream services (analytics, FHIR DetectedIssue, etc.) via Transponder.
/// </summary>
public sealed record AlarmRaisedIntegrationEvent(
    Ulid AlarmId,
    string? AlarmType,
    EventPhase EventPhase,
    AlarmState AlarmState,
    string? DeviceId,
    string? SessionId,
    DateTimeOffset OccurredAt,
    string? TenantId) : IntegrationEvent(AlarmId);
