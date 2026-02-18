using Transponder.Transports.Abstractions;

namespace Transponder.Transports;

/// <summary>
/// Default transport-level message envelope.
/// </summary>
public sealed class TransportMessage : ITransportMessage
{
    public TransportMessage(
        ReadOnlyMemory<byte> body,
        string? contentType,
        IReadOnlyDictionary<string, object?>? headers,
        MessageIdentity identity,
        string? messageType = null,
        DateTimeOffset? sentTime = null)
    {
        ArgumentNullException.ThrowIfNull(identity);

        Body = body;
        ContentType = contentType;
        Headers = headers ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        MessageId = identity.MessageId;
        CorrelationId = identity.CorrelationId;
        ConversationId = identity.ConversationId;
        MessageType = messageType;
        SentTime = sentTime;
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Body { get; }

    /// <inheritdoc />
    public string? ContentType { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Headers { get; }

    /// <inheritdoc />
    public Ulid? MessageId { get; }

    /// <inheritdoc />
    public Ulid? CorrelationId { get; }

    /// <inheritdoc />
    public Ulid? ConversationId { get; }

    /// <inheritdoc />
    public string? MessageType { get; }

    /// <inheritdoc />
    public DateTimeOffset? SentTime { get; }
}
