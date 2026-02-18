using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Domain.ValueObjects;

namespace Dialysis.Alarm.Application.Abstractions;

public sealed record AlarmInfo(
    Ulid? Id,
    string? AlarmType,
    string? SourceCode,
    string? SourceLimits,
    EventPhase EventPhase,
    AlarmState AlarmState,
    ActivityState ActivityState,
    AlarmPriority? Priority,
    string? DisplayName,
    DeviceId? DeviceId,
    string? SessionId,
    DateTimeOffset OccurredAt)
{
    public static AlarmInfo Create(AlarmCreateParams p) =>
        new(null, p.AlarmType, p.SourceCode, p.SourceLimits, p.State.EventPhase, p.State.AlarmState, p.State.ActivityState, p.Priority, p.DisplayName, p.DeviceId, p.SessionId, p.OccurredAt);
}
