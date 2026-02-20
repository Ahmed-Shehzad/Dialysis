namespace BuildingBlocks.Abstractions;

/// <summary>
/// Scoped buffer for integration events raised by domain event handlers during <c>SavingChangesAsync</c>.
/// Events are persisted to IntegrationEventOutbox in the same transaction and published by IntegrationEventOutboxPublisher.
/// preserving eventual consistency when handlers cannot add events to aggregates.
/// </summary>
public interface IIntegrationEventBuffer
{
    void Add(IIntegrationEvent integrationEvent);

    IReadOnlyList<IIntegrationEvent> Drain();
}
