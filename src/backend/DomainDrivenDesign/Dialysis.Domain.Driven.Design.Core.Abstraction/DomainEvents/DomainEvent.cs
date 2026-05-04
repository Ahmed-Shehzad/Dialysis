namespace Dialysis.DomainDrivenDesign.DomainEvents;

/// <summary>
/// Base type for immutable domain events with a default occurrence time.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent() => OccurredOn = DateTime.UtcNow;

    public DateTime OccurredOn { get; init; }
}
