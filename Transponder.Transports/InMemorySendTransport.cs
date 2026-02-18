using Transponder.Transports.Abstractions;

namespace Transponder.Transports;

internal sealed class InMemorySendTransport : ISendTransport
{
    private readonly TransportHostBase _host;
    private readonly Uri _address;

    public InMemorySendTransport(TransportHostBase host, Uri address)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _address = address ?? throw new ArgumentNullException(nameof(address));
    }

    public Task SendAsync(ITransportMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return !_host.TryGetEndpoint(_address, out ReceiveEndpoint endpoint)
            ? throw new InvalidOperationException($"No receive endpoint configured for '{_address}'.")
            : endpoint.HandleAsync(message, _host.Address, _address, cancellationToken);
    }
}
