using BuildingBlocks;

namespace Dialysis.Treatment.Application.Domain.Events;

public sealed record TreatmentSessionSignedEvent(
    Ulid TreatmentSessionId,
    SessionId SessionId,
    string? SignedBy) : DomainEvent;
