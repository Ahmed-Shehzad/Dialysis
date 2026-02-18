using Intercessor.Abstractions;

namespace BuildingBlocks.Abstractions;

/// <summary>
/// Defines a handler for an integration event.
/// </summary>
/// <typeparam name="TIntegrationEvent">The integration event type to handle.</typeparam>
public interface IIntegrationEventHandler<in TIntegrationEvent> : INotificationHandler<TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent;
