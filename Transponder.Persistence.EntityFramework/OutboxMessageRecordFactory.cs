using Transponder.Abstractions;
using Transponder.Persistence;
using Transponder.Transports.Abstractions;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// Factory to create <see cref="OutboxMessageRecord"/> from messages (e.g. integration events) for same-transaction outbox persistence.
/// </summary>
public static class OutboxMessageRecordFactory
{
    /// <summary>
    /// Creates an <see cref="OutboxMessageRecord"/> from an <see cref="IMessage"/>.
    /// </summary>
    public static OutboxMessageRecord CreateFromMessage(
        IMessage message,
        IMessageSerializer serializer,
        Uri sourceAddress,
        Ulid? correlationId = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(sourceAddress);

        Type messageType = message.GetType();
        ReadOnlyMemory<byte> body = serializer.Serialize(message, messageType);
        string typeName = messageType.FullName ?? messageType.Name;

        if (message is ICorrelatedMessage correlated && !correlationId.HasValue)
            correlationId = correlated.CorrelationId;

        var outboxMessage = new OutboxMessage(
            Ulid.NewUlid(),
            body,
            new OutboxMessageOptions
            {
                SourceAddress = sourceAddress,
                MessageType = typeName,
                ContentType = serializer.ContentType,
                CorrelationId = correlationId,
                EnqueuedTime = DateTimeOffset.UtcNow
            });

        return OutboxMessageRecord.FromMessage(outboxMessage);
    }
}
