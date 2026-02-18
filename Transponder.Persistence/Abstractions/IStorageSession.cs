namespace Transponder.Persistence.Abstractions;

/// <summary>
/// Represents a storage session coordinating inbox and outbox operations.
/// </summary>
public interface IStorageSession : IAsyncDisposable
{
    /// <summary>
    /// Gets the inbox store for this session.
    /// </summary>
    IInboxStore Inbox { get; }

    /// <summary>
    /// Gets the outbox store for this session.
    /// </summary>
    IOutboxStore Outbox { get; }

    /// <summary>
    /// Commits changes made within the session.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back changes made within the session.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
