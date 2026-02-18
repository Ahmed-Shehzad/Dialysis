using Transponder.Abstractions;
using Transponder.Persistence;
using Transponder.Transports.Abstractions;

namespace Transponder;

internal static class OutboxMessageFactory
{
    public static OutboxMessage Create<TMessage>(
        TMessage message,
        IMessageSerializer serializer,
        OutboxMessageFactoryOptions? options = null)
        where TMessage : class, IMessage
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(serializer);

        options ??= new OutboxMessageFactoryOptions();

        Type messageType = message.GetType();
        ReadOnlyMemory<byte> body = serializer.Serialize(message, messageType);
        string typeName = messageType.FullName ?? messageType.Name;
        Dictionary<string, object?> headerMap = options.Headers is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(options.Headers, StringComparer.OrdinalIgnoreCase);

        return new OutboxMessage(
            options.MessageId ?? Ulid.NewUlid(),
            body,
            new OutboxMessageOptions
            {
                Headers = headerMap,
                EnqueuedTime = DateTimeOffset.UtcNow,
                CorrelationId = options.CorrelationId,
                ConversationId = options.ConversationId,
                SourceAddress = options.SourceAddress,
                DestinationAddress = options.DestinationAddress,
                MessageType = typeName,
                ContentType = serializer.ContentType
            });
    }
}
