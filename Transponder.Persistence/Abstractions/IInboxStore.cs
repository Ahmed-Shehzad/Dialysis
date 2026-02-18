namespace Transponder.Persistence.Abstractions;

/// <summary>
/// Provides access to inbox state for de-duplication.
/// </summary>
public interface IInboxStore
{
    /// <summary>
    /// Gets the inbox state for a message and consumer.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="consumerId">The consumer identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task<IInboxState?> GetAsync(
        Ulid messageId,
        string consumerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to add a new inbox state entry.
    /// </summary>
    /// <param name="state">The inbox state to add.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns><c>true</c> if the entry was added; otherwise <c>false</c>.</returns>
    Task<bool> TryAddAsync(IInboxState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as processed for a consumer.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="consumerId">The consumer identifier.</param>
    /// <param name="processedTime">The time the message was processed.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task MarkProcessedAsync(
        Ulid messageId,
        string consumerId,
        DateTimeOffset processedTime,
        CancellationToken cancellationToken = default);
}
