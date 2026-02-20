using BuildingBlocks.Abstractions;

using Dialysis.Treatment.Application.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Treatment.Application.Features.ThresholdBreachDetected;

/// <summary>
/// In-process consumer for ThresholdBreachDetectedIntegrationEvent. Receives events post-commit.
/// Future: Alarm context could consume to create DetectedIssue or alarm.
/// </summary>
internal sealed class ThresholdBreachDetectedIntegrationEventConsumer : IIntegrationEventHandler<ThresholdBreachDetectedIntegrationEvent>
{
    private readonly ILogger<ThresholdBreachDetectedIntegrationEventConsumer> _logger;

    public ThresholdBreachDetectedIntegrationEventConsumer(ILogger<ThresholdBreachDetectedIntegrationEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ThresholdBreachDetectedIntegrationEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "IntegrationEvent: ThresholdBreachDetected SessionId={SessionId} Code={Code} BreachType={BreachType}",
            notification.SessionId,
            notification.Code,
            notification.BreachType);

        return Task.CompletedTask;
    }
}
