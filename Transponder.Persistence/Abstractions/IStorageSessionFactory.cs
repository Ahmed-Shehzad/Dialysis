namespace Transponder.Persistence.Abstractions;

/// <summary>
/// Creates storage sessions for inbox and outbox operations.
/// </summary>
public interface IStorageSessionFactory
{
    /// <summary>
    /// Creates a new storage session.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task<IStorageSession> CreateSessionAsync(CancellationToken cancellationToken = default);
}
