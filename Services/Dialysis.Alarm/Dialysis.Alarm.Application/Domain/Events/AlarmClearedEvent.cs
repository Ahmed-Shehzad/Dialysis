using BuildingBlocks;

namespace Dialysis.Alarm.Application.Domain.Events;

/// <summary>
/// Raised when an alarm condition is cleared (resolved).
/// </summary>
public sealed record AlarmClearedEvent(Ulid AlarmId) : DomainEvent;
