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
    string? InterpretationType,
    string? Abnormality,
    string? DisplayName,
    DeviceId? DeviceId,
    string? SessionId,
    DateTimeOffset OccurredAt,
    double? MessageTimeDriftSeconds = null)
{
    public static AlarmInfo Create(AlarmCreateParams p) =>
        new(null, p.AlarmType, p.SourceCode, p.SourceLimits, p.State.EventPhase, p.State.AlarmState, p.State.ActivityState, p.Priority, p.InterpretationType, p.Abnormality, p.DisplayName, p.DeviceId, p.SessionId, p.OccurredAt);
}
