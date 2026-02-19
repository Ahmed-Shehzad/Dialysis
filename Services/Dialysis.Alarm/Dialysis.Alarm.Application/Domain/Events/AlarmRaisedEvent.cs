using BuildingBlocks;
using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Domain.ValueObjects;

using SessionId = BuildingBlocks.ValueObjects.SessionId;

namespace Dialysis.Alarm.Application.Domain.Events;

public sealed record AlarmRaisedEvent(
    Ulid AlarmId,
    string? AlarmType,
    EventPhase EventPhase,
    AlarmState AlarmState,
    DeviceId? DeviceId,
    SessionId? SessionId,
    DateTimeOffset OccurredAt) : DomainEvent;
