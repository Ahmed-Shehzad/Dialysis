using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Domain.ValueObjects;

namespace Dialysis.Alarm.Application.Abstractions;

public sealed record AlarmInfo(
    Ulid? Id,
    string? AlarmType,
    string? SourceLimits,
    EventPhase EventPhase,
    AlarmState AlarmState,
    ActivityState ActivityState,
    DeviceId? DeviceId,
    string? SessionId,
    DateTimeOffset OccurredAt)
{
    public static AlarmInfo Create(
        string? alarmType,
        string? sourceLimits,
        AlarmStateDescriptor state,
        DeviceId? deviceId,
        string? sessionId,
        DateTimeOffset occurredAt) =>
        new(null, alarmType, sourceLimits, state.EventPhase, state.AlarmState, state.ActivityState, deviceId, sessionId, occurredAt);
}
