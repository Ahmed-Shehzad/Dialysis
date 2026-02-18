using BuildingBlocks.Abstractions;

using Dialysis.Treatment.Application.Contracts;
using Dialysis.Treatment.Application.Domain.Events;

using Transponder.Abstractions;

namespace Dialysis.Treatment.Application.Features.ObservationRecorded;

internal sealed class ObservationRecordedTransponderHandler : IDomainEventHandler<ObservationRecordedEvent>
{
    private readonly ISendEndpointProvider _sendEndpointProvider;

    public ObservationRecordedTransponderHandler(ISendEndpointProvider sendEndpointProvider)
    {
        _sendEndpointProvider = sendEndpointProvider;
    }

    public async Task HandleAsync(ObservationRecordedEvent notification, CancellationToken cancellationToken = default)
    {
        var message = new ObservationRecordedMessage(
            notification.SessionId,
            notification.ObservationId.ToString(),
            notification.Code.Value,
            notification.Value,
            notification.Unit,
            notification.SubId,
            notification.ChannelName);

        string groupName = $"session:{notification.SessionId}";
        Uri destination = new($"signalr://group/{groupName}");

        ISendEndpoint endpoint = await _sendEndpointProvider.GetSendEndpointAsync(destination, cancellationToken);
        await endpoint.SendAsync(message, cancellationToken);
    }
}
