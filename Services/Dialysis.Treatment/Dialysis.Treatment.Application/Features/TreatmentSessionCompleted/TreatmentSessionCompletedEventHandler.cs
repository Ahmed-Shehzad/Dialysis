using BuildingBlocks.Abstractions;

using Dialysis.Treatment.Application.Domain.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Treatment.Application.Features.TreatmentSessionCompleted;

internal sealed class TreatmentSessionCompletedEventHandler : IDomainEventHandler<TreatmentSessionCompletedEvent>
{
    private readonly ILogger<TreatmentSessionCompletedEventHandler> _logger;

    public TreatmentSessionCompletedEventHandler(ILogger<TreatmentSessionCompletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(TreatmentSessionCompletedEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DomainEvent: TreatmentSessionCompleted TreatmentSessionId={TreatmentSessionId} SessionId={SessionId}",
            notification.TreatmentSessionId,
            notification.SessionId.Value);

        return Task.CompletedTask;
    }
}
