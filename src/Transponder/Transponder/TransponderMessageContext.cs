namespace Transponder;

/// <summary>
/// Captures message metadata for diagnostics scopes.
/// </summary>
public sealed class TransponderMessageContext
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyHeaders =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public TransponderMessageContext(
        Ulid? messageId,
        Ulid? correlationId,
        Ulid? conversationId,
        string? messageType,
        Uri? sourceAddress,
        Uri? destinationAddress,
        DateTimeOffset? sentTime,
        IReadOnlyDictionary<string, object?>? headers)
    {
        MessageId = messageId;
        CorrelationId = correlationId;
        ConversationId = conversationId;
        MessageType = messageType;
        SourceAddress = sourceAddress;
        DestinationAddress = destinationAddress;
        SentTime = sentTime;
        Headers = headers ?? EmptyHeaders;
    }

    public Ulid? MessageId { get; }

    public Ulid? CorrelationId { get; }

    public Ulid? ConversationId { get; }

    public string? MessageType { get; }

    public Uri? SourceAddress { get; }

    public Uri? DestinationAddress { get; }

    public DateTimeOffset? SentTime { get; }

    public IReadOnlyDictionary<string, object?> Headers { get; }
}
