using BuildingBlocks.Abstractions;

using Dialysis.Treatment.Application.Domain.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Treatment.Application.Features.TreatmentSessionSigned;

internal sealed class TreatmentSessionSignedEventHandler : IDomainEventHandler<TreatmentSessionSignedEvent>
{
    private readonly ILogger<TreatmentSessionSignedEventHandler> _logger;

    public TreatmentSessionSignedEventHandler(ILogger<TreatmentSessionSignedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(TreatmentSessionSignedEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DomainEvent: TreatmentSessionSigned TreatmentSessionId={TreatmentSessionId} SessionId={SessionId} SignedBy={SignedBy}",
            notification.TreatmentSessionId,
            notification.SessionId.Value,
            notification.SignedBy ?? "(unknown)");

        return Task.CompletedTask;
    }
}
