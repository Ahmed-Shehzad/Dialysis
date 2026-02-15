namespace Transponder.Persistence.Abstractions;

/// <summary>
/// Represents the processing state of a consumed message for inbox de-duplication.
/// </summary>
public interface IInboxState
{
    /// <summary>
    /// Gets the message identifier.
    /// </summary>
    Ulid MessageId { get; }

    /// <summary>
    /// Gets the consumer identifier used for de-duplication.
    /// </summary>
    string ConsumerId { get; }

    /// <summary>
    /// Gets the time the message was received.
    /// </summary>
    DateTimeOffset ReceivedTime { get; }

    /// <summary>
    /// Gets the time the message was processed, if completed.
    /// </summary>
    DateTimeOffset? ProcessedTime { get; }
}
