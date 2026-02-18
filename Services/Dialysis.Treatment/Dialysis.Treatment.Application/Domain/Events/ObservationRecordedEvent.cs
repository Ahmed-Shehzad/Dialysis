using BuildingBlocks;

using Dialysis.Treatment.Application.Domain.ValueObjects;

namespace Dialysis.Treatment.Application.Domain.Events;

public sealed record ObservationRecordedEvent(
    Ulid TreatmentSessionId,
    Ulid ObservationId,
    ObservationCode Code,
    string? Value,
    string? Unit) : DomainEvent;
