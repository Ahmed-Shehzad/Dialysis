using BuildingBlocks;

namespace Dialysis.Treatment.Application.Domain.Events;

public sealed record TreatmentSessionCompletedEvent(
    Ulid TreatmentSessionId,
    SessionId SessionId) : DomainEvent;
