namespace Transponder.Persistence.Abstractions;

/// <summary>
/// Provides access to persisted scheduled messages.
/// </summary>
public interface IScheduledMessageStore
{
    /// <summary>
    /// Adds a scheduled message.
    /// </summary>
    Task AddAsync(IScheduledMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets due scheduled messages.
    /// </summary>
    Task<IReadOnlyList<IScheduledMessage>> GetDueAsync(
        DateTimeOffset now,
        int maxCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a scheduled message as dispatched.
    /// </summary>
    Task MarkDispatchedAsync(
        Ulid tokenId,
        DateTimeOffset dispatchedTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a scheduled message.
    /// </summary>
    Task<bool> CancelAsync(Ulid tokenId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a scheduled message by token id.
    /// </summary>
    Task<IScheduledMessage?> GetAsync(Ulid tokenId, CancellationToken cancellationToken = default);
}
