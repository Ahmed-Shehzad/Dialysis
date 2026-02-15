using Transponder.Abstractions;
using Transponder.Transports;
using Transponder.Transports.Abstractions;

namespace Transponder;

internal static class TransportMessageFactory
{
    public static TransportMessage Create<TMessage>(
        TMessage message,
        IMessageSerializer serializer,
        Ulid? messageId = null,
        Ulid? correlationId = null,
        Ulid? conversationId = null,
        IReadOnlyDictionary<string, object?>? headers = null,
        DateTimeOffset? sentTime = null)
        where TMessage : class, IMessage
        => Create((object)message, serializer, messageId, correlationId, conversationId, headers, sentTime);

    public static TransportMessage Create(
        object message,
        IMessageSerializer serializer,
        Ulid? messageId = null,
        Ulid? correlationId = null,
        Ulid? conversationId = null,
        IReadOnlyDictionary<string, object?>? headers = null,
        DateTimeOffset? sentTime = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(serializer);

        var messageType = message.GetType();
        var body = serializer.Serialize(message, messageType);
        var headerMap = headers is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(headers, StringComparer.OrdinalIgnoreCase);

        return new TransportMessage(
            body,
            serializer.ContentType,
            headerMap,
            messageId ?? Ulid.NewUlid(),
            correlationId,
            conversationId,
            messageType.FullName,
            sentTime ?? DateTimeOffset.UtcNow);
    }
}
