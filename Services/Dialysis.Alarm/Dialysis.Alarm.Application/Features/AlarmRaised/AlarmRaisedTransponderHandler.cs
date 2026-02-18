using BuildingBlocks.Abstractions;

using Dialysis.Alarm.Application.Contracts;
using Dialysis.Alarm.Application.Domain.Events;

using Transponder.Abstractions;

namespace Dialysis.Alarm.Application.Features.AlarmRaised;

internal sealed class AlarmRaisedTransponderHandler : IDomainEventHandler<AlarmRaisedEvent>
{
    private readonly ISendEndpointProvider _sendEndpointProvider;

    public AlarmRaisedTransponderHandler(ISendEndpointProvider sendEndpointProvider)
    {
        _sendEndpointProvider = sendEndpointProvider;
    }

    public async Task HandleAsync(AlarmRaisedEvent notification, CancellationToken cancellationToken = default)
    {
        var message = new AlarmRecordedMessage(
            notification.AlarmId.ToString(),
            notification.AlarmType,
            notification.EventPhase.Value,
            notification.AlarmState.Value,
            notification.DeviceId?.Value,
            notification.SessionId,
            notification.OccurredAt);

        string groupName = !string.IsNullOrEmpty(notification.SessionId)
            ? $"session:{notification.SessionId}"
            : $"device:{notification.DeviceId?.Value ?? "unknown"}";
        Uri destination = new($"signalr://group/{groupName}");

        ISendEndpoint endpoint = await _sendEndpointProvider.GetSendEndpointAsync(destination, cancellationToken);
        await endpoint.SendAsync(message, cancellationToken);
    }
}
