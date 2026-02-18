using BuildingBlocks;
using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Domain.ValueObjects;

namespace Dialysis.Alarm.Application.Domain.Events;

public sealed record AlarmRaisedEvent(
    Ulid AlarmId,
    string? AlarmType,
    EventPhase EventPhase,
    AlarmState AlarmState,
    DeviceId? DeviceId,
    string? SessionId,
    DateTimeOffset OccurredAt) : DomainEvent;
