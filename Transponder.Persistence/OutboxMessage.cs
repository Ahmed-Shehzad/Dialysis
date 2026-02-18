using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence;

/// <summary>
/// Default outbox message implementation.
/// </summary>
public sealed class OutboxMessage : IOutboxMessage
{
    private readonly byte[] _body;
    private readonly IReadOnlyDictionary<string, object?> _headers;

    public OutboxMessage(
        Ulid messageId,
        ReadOnlyMemory<byte> body,
        OutboxMessageOptions? options = null)
    {
        if (messageId == Ulid.Empty) throw new ArgumentException("MessageId must be provided.", nameof(messageId));

        options ??= new OutboxMessageOptions();

        MessageId = messageId;
        CorrelationId = options.CorrelationId;
        ConversationId = options.ConversationId;
        SourceAddress = options.SourceAddress;
        DestinationAddress = options.DestinationAddress;
        MessageType = options.MessageType;
        ContentType = options.ContentType;
        _body = body.ToArray();
        _headers = options.Headers != null
            ? new Dictionary<string, object?>(options.Headers, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        EnqueuedTime = options.EnqueuedTime ?? DateTimeOffset.UtcNow;
        SentTime = options.SentTime;
    }

    /// <inheritdoc />
    public Ulid MessageId { get; }

    /// <inheritdoc />
    public Ulid? CorrelationId { get; }

    /// <inheritdoc />
    public Ulid? ConversationId { get; }

    /// <inheritdoc />
    public Uri? SourceAddress { get; }

    /// <inheritdoc />
    public Uri? DestinationAddress { get; }

    /// <inheritdoc />
    public string? MessageType { get; }

    /// <inheritdoc />
    public string? ContentType { get; }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Body => _body;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Headers => _headers;

    /// <inheritdoc />
    public DateTimeOffset EnqueuedTime { get; }

    /// <inheritdoc />
    public DateTimeOffset? SentTime { get; private set; }

    internal void MarkSent(DateTimeOffset sentTime) => SentTime = sentTime;

    public static OutboxMessage FromMessage(IOutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new OutboxMessage(
            message.MessageId,
            message.Body,
            new OutboxMessageOptions
            {
                Headers = message.Headers,
                EnqueuedTime = message.EnqueuedTime,
                CorrelationId = message.CorrelationId,
                ConversationId = message.ConversationId,
                SourceAddress = message.SourceAddress,
                DestinationAddress = message.DestinationAddress,
                MessageType = message.MessageType,
                ContentType = message.ContentType,
                SentTime = message.SentTime
            });
    }
}

public sealed class OutboxMessageOptions
{
    public IReadOnlyDictionary<string, object?>? Headers { get; init; }

    public DateTimeOffset? EnqueuedTime { get; init; }

    public Ulid? CorrelationId { get; init; }

    public Ulid? ConversationId { get; init; }

    public Uri? SourceAddress { get; init; }

    public Uri? DestinationAddress { get; init; }

    public string? MessageType { get; init; }

    public string? ContentType { get; init; }

    public DateTimeOffset? SentTime { get; init; }
}
