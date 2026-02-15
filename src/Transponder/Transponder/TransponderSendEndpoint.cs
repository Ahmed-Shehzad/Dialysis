using Transponder.Abstractions;

namespace Transponder;

internal sealed class TransponderSendEndpoint : ISendEndpoint
{
    private readonly TransponderBus _bus;
    private readonly Uri _address;

    public TransponderSendEndpoint(TransponderBus bus, Uri address)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _address = address ?? throw new ArgumentNullException(nameof(address));
    }

    public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
        => _bus.SendInternalAsync(_address, message, null, null, null, null, cancellationToken);
}
