using BuildingBlocks.Abstractions;

namespace BuildingBlocks;

/// <summary>
/// Base type for domain events with standard metadata.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Ulid EventId { get; init; } = Ulid.NewUlid();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
