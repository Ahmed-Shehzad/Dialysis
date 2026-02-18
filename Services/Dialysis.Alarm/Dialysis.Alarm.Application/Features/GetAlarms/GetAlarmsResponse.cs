namespace Dialysis.Alarm.Application.Features.GetAlarms;

public sealed record GetAlarmsResponse(IReadOnlyList<AlarmDto> Alarms);

public sealed record AlarmDto(
    string Id,
    string? AlarmType,
    string? SourceCode,
    string? SourceLimits,
    string? Priority,
    string? InterpretationType,
    string? Abnormality,
    string EventPhase,
    string AlarmState,
    string ActivityState,
    string? DeviceId,
    string? SessionId,
    DateTimeOffset OccurredAt);
