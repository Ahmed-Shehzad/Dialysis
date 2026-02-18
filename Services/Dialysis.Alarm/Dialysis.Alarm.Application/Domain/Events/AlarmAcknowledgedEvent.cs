using BuildingBlocks;

namespace Dialysis.Alarm.Application.Domain.Events;

/// <summary>
/// Raised when an alarm is acknowledged by a clinician.
/// </summary>
public sealed record AlarmAcknowledgedEvent(Ulid AlarmId) : DomainEvent;
