using BuildingBlocks;
using BuildingBlocks.ValueObjects;

using SessionId = BuildingBlocks.ValueObjects.SessionId;

namespace Dialysis.Alarm.Application.Domain.Events;

/// <summary>
/// Raised when an alarm is raised; triggers escalation evaluation (e.g. 3+ active alarms in 5 min).
/// </summary>
public sealed record AlarmEscalationCheckEvent(
    Ulid AlarmId,
    DeviceId? DeviceId,
    SessionId? SessionId,
    DateTimeOffset OccurredAt) : DomainEvent;
