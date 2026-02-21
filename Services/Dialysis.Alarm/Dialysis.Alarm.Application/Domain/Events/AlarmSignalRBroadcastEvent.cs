using BuildingBlocks;
using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Domain.ValueObjects;

using SessionId = BuildingBlocks.ValueObjects.SessionId;

namespace Dialysis.Alarm.Application.Domain.Events;

/// <summary>
/// Raised when an alarm is raised; triggers real-time broadcast via SignalR.
/// </summary>
public sealed record AlarmSignalRBroadcastEvent(
    Ulid AlarmId,
    string? AlarmType,
    EventPhase EventPhase,
    AlarmState AlarmState,
    DeviceId? DeviceId,
    SessionId? SessionId,
    DateTimeOffset OccurredAt) : DomainEvent;
