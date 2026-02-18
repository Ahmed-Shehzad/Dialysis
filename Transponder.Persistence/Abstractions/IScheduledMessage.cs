namespace Transponder.Persistence.Abstractions;

/// <summary>
/// Represents a stored scheduled message.
/// </summary>
public interface IScheduledMessage
{
    /// <summary>
    /// Gets the scheduling token identifier.
    /// </summary>
    Ulid TokenId { get; }

    /// <summary>
    /// Gets the message type identifier.
    /// </summary>
    string MessageType { get; }

    /// <summary>
    /// Gets the content type of the message payload.
    /// </summary>
    string? ContentType { get; }

    /// <summary>
    /// Gets the message payload bytes.
    /// </summary>
    ReadOnlyMemory<byte> Body { get; }

    /// <summary>
    /// Gets the message headers.
    /// </summary>
    IReadOnlyDictionary<string, object?> Headers { get; }

    /// <summary>
    /// Gets the time the message should be published.
    /// </summary>
    DateTimeOffset ScheduledTime { get; }

    /// <summary>
    /// Gets the time the message was created.
    /// </summary>
    DateTimeOffset CreatedTime { get; }

    /// <summary>
    /// Gets the time the message was dispatched, if completed.
    /// </summary>
    DateTimeOffset? DispatchedTime { get; }
}
