namespace Transponder.Transports.Abstractions;

/// <summary>
/// Represents a transport-level message envelope.
/// </summary>
public interface ITransportMessage
{
    /// <summary>
    /// Gets the message payload bytes.
    /// </summary>
    ReadOnlyMemory<byte> Body { get; }

    /// <summary>
    /// Gets the content type of the message payload.
    /// </summary>
    string? ContentType { get; }

    /// <summary>
    /// Gets the message headers.
    /// </summary>
    IReadOnlyDictionary<string, object?> Headers { get; }

    /// <summary>
    /// Gets the message identifier.
    /// </summary>
    Ulid? MessageId { get; }

    /// <summary>
    /// Gets the correlation identifier used to relate messages.
    /// </summary>
    Ulid? CorrelationId { get; }

    /// <summary>
    /// Gets the conversation identifier for a logical flow of messages.
    /// </summary>
    Ulid? ConversationId { get; }

    /// <summary>
    /// Gets the message type name or identifier.
    /// </summary>
    string? MessageType { get; }

    /// <summary>
    /// Gets the time the message was sent, if known.
    /// </summary>
    DateTimeOffset? SentTime { get; }
}
