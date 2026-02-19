using Grpc.Net.Client;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Grpc;

internal sealed class GrpcSendTransport : ISendTransport
{
    private readonly Transport.TransportClient _client;
    private readonly Uri _destination;

    public GrpcSendTransport(GrpcChannel channel, Uri destination)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _destination = destination ?? throw new ArgumentNullException(nameof(destination));
        _client = new Transport.TransportClient(channel);
    }

    public async Task SendAsync(ITransportMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var request = new SendRequest
        {
            Destination = _destination.ToString(),
            Message = GrpcTransportMessageMapper.ToProto(message)
        };

#if DEBUG
        System.Diagnostics.Trace.TraceInformation(
            $"[Transponder] GrpcSendTransport sending to {_destination} messageType={message.MessageType ?? "unknown"}.");
#endif
        _ = await _client.SendAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);
#if DEBUG
        System.Diagnostics.Trace.TraceInformation(
            $"[Transponder] GrpcSendTransport sent to {_destination} messageType={message.MessageType ?? "unknown"}.");
#endif
    }
}
