namespace Dialysis.DomainDrivenDesign.DomainEvents;

/// <summary>
/// Handles a single <typeparamref name="TEvent"/> raised by an aggregate within the bounded context.
/// Multiple handlers may be registered per event; the dispatcher invokes them all and aggregates failures.
/// </summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
