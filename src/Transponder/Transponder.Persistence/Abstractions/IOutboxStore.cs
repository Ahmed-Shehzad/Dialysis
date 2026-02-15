namespace Transponder.Persistence.Abstractions;

/// <summary>
/// Provides access to stored outbox messages.
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// Adds a message to the outbox for later dispatch.
    /// </summary>
    /// <param name="message">The outbox message.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending outbox messages for dispatch.
    /// </summary>
    /// <param name="maxCount">The maximum number of messages to return.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task<IReadOnlyList<IOutboxMessage>> GetPendingAsync(
        int maxCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as sent.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="sentTime">The time the message was sent.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task MarkSentAsync(Ulid messageId, DateTimeOffset sentTime, CancellationToken cancellationToken = default);
}
