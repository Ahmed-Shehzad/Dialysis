using Grpc.Net.Client;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Grpc;

internal sealed class GrpcPublishTransport : IPublishTransport
{
    private readonly Transport.TransportClient _client;

    public GrpcPublishTransport(GrpcChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _client = new Transport.TransportClient(channel);
    }

    public async Task PublishAsync(ITransportMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var request = new PublishRequest
        {
            Message = GrpcTransportMessageMapper.ToProto(message)
        };

        _ = await _client.PublishAsync(request, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
