using BuildingBlocks.Abstractions;
using Dialysis.Contracts.Events;
using Intercessor.Abstractions;

namespace Dialysis.Prediction;

/// <summary>
/// Handles ObservationCreated from the message bus and dispatches to Intercessor.
/// </summary>
public sealed class ObservationCreatedIntegrationEventHandler : IIntegrationEventHandler<ObservationCreated>
{
    private readonly IPublisher _publisher;

    public ObservationCreatedIntegrationEventHandler(IPublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public Task HandleAsync(ObservationCreated message, CancellationToken cancellationToken = default)
        => _publisher.PublishAsync(message, cancellationToken);
}
