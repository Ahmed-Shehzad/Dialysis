using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence;

/// <summary>
/// In-memory storage session for inbox/outbox operations.
/// </summary>
public sealed class InMemoryStorageSession : IStorageSession
{
    public InMemoryStorageSession(IInboxStore inbox, IOutboxStore outbox)
    {
        Inbox = inbox ?? throw new ArgumentNullException(nameof(inbox));
        Outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    /// <inheritdoc />
    public IInboxStore Inbox { get; }

    /// <inheritdoc />
    public IOutboxStore Outbox { get; }

    /// <inheritdoc />
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
