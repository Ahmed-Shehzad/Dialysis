using Transponder.Abstractions;
using Transponder.Transports.Abstractions;

namespace Transponder;

/// <summary>
/// Default consume context implementation.
/// </summary>
public sealed class ConsumeContext<TMessage> : IConsumeContext<TMessage>
    where TMessage : class, IMessage
{
    private readonly TransponderBus _bus;
    private readonly Uri? _responseAddress;

    public ConsumeContext(
        TMessage message,
        ITransportMessage transportMessage,
        Uri? sourceAddress,
        Uri? destinationAddress,
        CancellationToken cancellationToken,
        TransponderBus bus)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        ArgumentNullException.ThrowIfNull(transportMessage);
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));

        MessageId = transportMessage.MessageId;
        CorrelationId = transportMessage.CorrelationId;
        ConversationId = transportMessage.ConversationId;
        SourceAddress = sourceAddress;
        DestinationAddress = destinationAddress;
        SentTime = transportMessage.SentTime;
        Headers = transportMessage.Headers;
        CancellationToken = cancellationToken;
        _responseAddress = ResolveResponseAddress(transportMessage, sourceAddress);
    }

    public TMessage Message { get; }

    public Ulid? MessageId { get; }

    public Ulid? CorrelationId { get; }

    public Ulid? ConversationId { get; }

    public Uri? SourceAddress { get; }

    public Uri? DestinationAddress { get; }

    public DateTimeOffset? SentTime { get; }

    public IReadOnlyDictionary<string, object?> Headers { get; }

    public CancellationToken CancellationToken { get; }

    public Task PublishAsync<TMessageOut>(TMessageOut message, CancellationToken cancellationToken = default)
        where TMessageOut : class, IMessage
        => _bus.PublishInternalAsync(message, CorrelationId, ConversationId, null, cancellationToken);

    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        ISendEndpoint endpoint = new ConsumeSendEndpoint(_bus, address, CorrelationId, ConversationId);
        return Task.FromResult(endpoint);
    }

    public Task RespondAsync<TResponse>(TResponse response, CancellationToken cancellationToken = default)
        where TResponse : class, IMessage
    {
        if (_responseAddress is null)
            // Note: Logger would need to be injected, but ConsumeContext is created internally
            // For now, we'll throw the exception without logging to avoid breaking changes
            throw new InvalidOperationException($"Response address is not available for this context. MessageType={typeof(TMessage).Name}");

        var headers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (MessageId.HasValue) headers[TransponderMessageHeaders.RequestId] = MessageId.Value.ToString();

        return _bus.SendInternalAsync(
            _responseAddress,
            response,
            null,
            MessageId,
            ConversationId,
            headers,
            cancellationToken);
    }

    private static Uri? ResolveResponseAddress(ITransportMessage transportMessage, Uri? sourceAddress)
    {
        return transportMessage.Headers.TryGetValue(TransponderMessageHeaders.ResponseAddress, out object? value) &&
            Uri.TryCreate(value?.ToString(), UriKind.RelativeOrAbsolute, out Uri? parsed)
            ? parsed
            : sourceAddress;
    }
}
