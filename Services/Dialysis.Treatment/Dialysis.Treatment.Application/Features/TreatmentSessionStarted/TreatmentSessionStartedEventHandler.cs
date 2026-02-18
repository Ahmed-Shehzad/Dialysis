using BuildingBlocks.Abstractions;

using Dialysis.Treatment.Application.Domain.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Treatment.Application.Features.TreatmentSessionStarted;

internal sealed class TreatmentSessionStartedEventHandler : IDomainEventHandler<TreatmentSessionStartedEvent>
{
    private readonly ILogger<TreatmentSessionStartedEventHandler> _logger;

    public TreatmentSessionStartedEventHandler(ILogger<TreatmentSessionStartedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(TreatmentSessionStartedEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DomainEvent: TreatmentSessionStarted TreatmentSessionId={TreatmentSessionId} SessionId={SessionId} PatientMrn={PatientMrn} DeviceId={DeviceId}",
            notification.TreatmentSessionId,
            notification.SessionId.Value,
            notification.PatientMrn?.Value,
            notification.DeviceId?.Value);

        return Task.CompletedTask;
    }
}
