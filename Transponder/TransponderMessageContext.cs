namespace Transponder;

/// <summary>
/// Captures message metadata for diagnostics scopes.
/// </summary>
public sealed class TransponderMessageContext
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyHeaders =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public TransponderMessageContext(
        Transports.MessageIdentity identity,
        string? messageType,
        MessageAddressing addressing,
        DateTimeOffset? sentTime,
        IReadOnlyDictionary<string, object?>? headers)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(addressing);

        MessageId = identity.MessageId;
        CorrelationId = identity.CorrelationId;
        ConversationId = identity.ConversationId;
        MessageType = messageType;
        SourceAddress = addressing.SourceAddress;
        DestinationAddress = addressing.DestinationAddress;
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
