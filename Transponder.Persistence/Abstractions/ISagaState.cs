namespace Transponder.Persistence.Abstractions;

/// <summary>
/// Represents persisted state for a saga instance.
/// </summary>
public interface ISagaState
{
    /// <summary>
    /// Gets or sets the correlation identifier for the saga instance.
    /// </summary>
    Ulid CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the conversation identifier for the saga instance.
    /// </summary>
    Ulid? ConversationId { get; set; }

    /// <summary>
    /// Gets or sets the version number for optimistic concurrency control.
    /// </summary>
    int Version { get; set; }
}
