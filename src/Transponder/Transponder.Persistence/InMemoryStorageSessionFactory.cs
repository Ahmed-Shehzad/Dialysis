using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence;

/// <summary>
/// Creates in-memory storage sessions.
/// </summary>
public sealed class InMemoryStorageSessionFactory : IStorageSessionFactory
{
    private readonly InMemoryInboxStore _inbox;
    private readonly InMemoryOutboxStore _outbox;

    public InMemoryStorageSessionFactory(
        InMemoryInboxStore? inbox = null,
        InMemoryOutboxStore? outbox = null)
    {
        _inbox = inbox ?? new InMemoryInboxStore();
        _outbox = outbox ?? new InMemoryOutboxStore();
    }

    /// <inheritdoc />
    public Task<IStorageSession> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IStorageSession session = new InMemoryStorageSession(_inbox, _outbox);
        return Task.FromResult(session);
    }
}
