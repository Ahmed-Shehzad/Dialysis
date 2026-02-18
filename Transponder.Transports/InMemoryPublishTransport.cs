using Transponder.Transports.Abstractions;

namespace Transponder.Transports;

internal sealed class InMemoryPublishTransport : IPublishTransport
{
    private readonly TransportHostBase _host;

    public InMemoryPublishTransport(TransportHostBase host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public async Task PublishAsync(ITransportMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        IReadOnlyCollection<ReceiveEndpoint> endpoints = _host.GetEndpoints();

        foreach (ReceiveEndpoint endpoint in endpoints)
            await endpoint.HandleAsync(message, _host.Address, endpoint.InputAddress, cancellationToken)
                .ConfigureAwait(false);
    }
}
