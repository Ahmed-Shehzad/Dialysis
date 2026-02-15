namespace BuildingBlocks.Abstractions;

/// <summary>
/// Handles integration events received from the message bus (e.g. via Transponder/Azure Service Bus).
/// Implement this in consumer projects to process cross-service events.
/// </summary>
/// <typeparam name="TIntegrationEvent">The integration event type to handle.</typeparam>
public interface IIntegrationEventHandler<in TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    Task HandleAsync(TIntegrationEvent message, CancellationToken cancellationToken = default);
}
