namespace Dialysis.DomainDrivenDesign.DomainEvents;

/// <summary>
/// Resolves and invokes <see cref="IDomainEventHandler{TEvent}"/> instances for a raised
/// <see cref="IDomainEvent"/>. Typically called by persistence infrastructure after the
/// transactional commit so handlers run only when the aggregate change is durable.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
