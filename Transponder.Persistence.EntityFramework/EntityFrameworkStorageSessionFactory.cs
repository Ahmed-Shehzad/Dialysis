using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Transponder.Persistence.Abstractions;
using Transponder.Persistence.EntityFramework.Abstractions;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// Creates Entity Framework storage sessions.
/// </summary>
public sealed class EntityFrameworkStorageSessionFactory<TContext> : IStorageSessionFactory
    where TContext : DbContext
{
    private readonly IEntityFrameworkDbContextFactory<TContext> _contextFactory;
    private readonly bool _useTransaction;

    public EntityFrameworkStorageSessionFactory(
        IEntityFrameworkDbContextFactory<TContext> contextFactory,
        bool useTransaction = true)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _useTransaction = useTransaction;
    }

    /// <inheritdoc />
    public async Task<IStorageSession> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        TContext context = _contextFactory.CreateDbContext();

        IDbContextTransaction? transaction = _useTransaction
            ? await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
            : null;

        return new EntityFrameworkStorageSession(context, transaction);
    }
}
