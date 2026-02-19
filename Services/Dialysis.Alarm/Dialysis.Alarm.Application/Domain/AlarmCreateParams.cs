using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Domain.ValueObjects;

using SessionId = BuildingBlocks.ValueObjects.SessionId;

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
    SessionId? SessionId,
    DateTimeOffset OccurredAt);
