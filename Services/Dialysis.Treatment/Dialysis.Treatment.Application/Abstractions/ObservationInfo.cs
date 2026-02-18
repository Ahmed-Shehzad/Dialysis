using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Abstractions;

public sealed record ObservationInfo(
    ObservationCode Code,
    string? Value,
    string? Unit,
    string? SubId,
    string? ReferenceRange,
    ObservationStatus? ResultStatus,
    DateTimeOffset? EffectiveTime,
    string? Provenance,
    string? EquipmentInstanceId,
    ContainmentLevel? Level);
