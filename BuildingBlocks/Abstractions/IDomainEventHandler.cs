using Intercessor.Abstractions;

namespace BuildingBlocks.Abstractions;

/// <summary>
/// Defines a handler for a domain event.
/// </summary>
/// <typeparam name="TDomainEvent">The domain event type to handle.</typeparam>
public interface IDomainEventHandler<in TDomainEvent> : INotificationHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent;
