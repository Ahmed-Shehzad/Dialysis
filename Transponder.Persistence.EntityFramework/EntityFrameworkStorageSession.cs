using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// Entity Framework storage session for inbox/outbox operations.
/// </summary>
public sealed class EntityFrameworkStorageSession : IStorageSession
{
    private readonly DbContext _context;
    private readonly IDbContextTransaction? _transaction;
    private bool _disposed;

    public EntityFrameworkStorageSession(DbContext context, IDbContextTransaction? transaction)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _transaction = transaction;
        Inbox = new EntityFrameworkInboxStore(_context);
        Outbox = new EntityFrameworkOutboxStore(_context);
    }

    /// <inheritdoc />
    public IInboxStore Inbox { get; }

    /// <inheritdoc />
    public IOutboxStore Outbox { get; }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _ = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (_transaction != null) await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_transaction != null) await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

        _context.ChangeTracker.Clear();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;

        if (_transaction != null) await _transaction.DisposeAsync().ConfigureAwait(false);

        await _context.DisposeAsync().ConfigureAwait(false);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(EntityFrameworkStorageSession));
    }
}
