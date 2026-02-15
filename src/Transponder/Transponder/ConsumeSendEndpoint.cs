using Transponder.Abstractions;

namespace Transponder;

internal sealed class ConsumeSendEndpoint : ISendEndpoint
{
    private readonly TransponderBus _bus;
    private readonly Uri _address;
    private readonly Ulid? _correlationId;
    private readonly Ulid? _conversationId;

    public ConsumeSendEndpoint(
        TransponderBus bus,
        Uri address,
        Ulid? correlationId,
        Ulid? conversationId)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _address = address ?? throw new ArgumentNullException(nameof(address));
        _correlationId = correlationId;
        _conversationId = conversationId;
    }

    public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IMessage
        => _bus.SendInternalAsync(
            _address,
            message,
            null,
            _correlationId,
            _conversationId,
            null,
            cancellationToken);
}
