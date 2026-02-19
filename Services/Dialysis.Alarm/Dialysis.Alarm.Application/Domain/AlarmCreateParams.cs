using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Domain.ValueObjects;

namespace Dialysis.Alarm.Application.Domain;

public sealed record AlarmCreateParams(
    string? AlarmType,
    string? SourceCode,
    string? SourceLimits,
    AlarmStateDescriptor State,
    AlarmPriority? Priority,
    string? InterpretationType,
    string? Abnormality,
    string? DisplayName,
    DeviceId? DeviceId,
    string? SessionId,
    DateTimeOffset OccurredAt);
