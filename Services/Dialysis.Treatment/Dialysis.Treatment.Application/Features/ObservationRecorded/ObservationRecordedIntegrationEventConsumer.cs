using BuildingBlocks.Abstractions;

using Dialysis.Treatment.Application.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Treatment.Application.Features.ObservationRecorded;

/// <summary>
/// In-process consumer for ObservationRecordedIntegrationEvent. Demonstrates IIntegrationEventHandler pattern.
/// </summary>
internal sealed class ObservationRecordedIntegrationEventConsumer : IIntegrationEventHandler<ObservationRecordedIntegrationEvent>
{
    private readonly ILogger<ObservationRecordedIntegrationEventConsumer> _logger;

    public ObservationRecordedIntegrationEventConsumer(ILogger<ObservationRecordedIntegrationEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ObservationRecordedIntegrationEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "IntegrationEvent: ObservationRecorded SessionId={SessionId} Code={Code} Value={Value}",
            notification.SessionId,
            notification.Code.Value,
            notification.Value);

        return Task.CompletedTask;
    }
}
