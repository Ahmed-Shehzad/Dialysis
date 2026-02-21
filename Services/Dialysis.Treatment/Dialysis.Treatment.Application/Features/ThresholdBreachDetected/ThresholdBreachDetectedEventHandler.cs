using BuildingBlocks.Abstractions;

using Dialysis.Treatment.Application.Domain.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Treatment.Application.Features.ThresholdBreachDetected;

/// <summary>
/// Logs threshold breach domain events. Integration event is raised by TreatmentSession aggregate.
/// </summary>
internal sealed class ThresholdBreachDetectedEventHandler : IDomainEventHandler<ThresholdBreachDetectedEvent>
{
    private readonly ILogger<ThresholdBreachDetectedEventHandler> _logger;

    public ThresholdBreachDetectedEventHandler(ILogger<ThresholdBreachDetectedEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task HandleAsync(ThresholdBreachDetectedEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "DomainEvent: ThresholdBreachDetected SessionId={SessionId} ObservationId={ObservationId} Code={Code} BreachType={BreachType} Value={Value} Threshold={Threshold} Direction={Direction}",
            notification.SessionId,
            notification.ObservationId,
            notification.Code.Value,
            notification.Breach.BreachType,
            notification.Breach.ObservedValue,
            notification.Breach.ThresholdValue,
            notification.Breach.Direction);

        return Task.CompletedTask;
    }
}
