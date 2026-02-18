using Microsoft.EntityFrameworkCore;

using Transponder.Persistence.EntityFramework.Abstractions;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// Adapts EF Core's DbContext factory to Transponder's persistence abstraction.
/// </summary>
public sealed class EntityFrameworkDbContextFactory<TContext> : IEntityFrameworkDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly IDbContextFactory<TContext> _factory;

    public EntityFrameworkDbContextFactory(IDbContextFactory<TContext> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public TContext CreateDbContext() => _factory.CreateDbContext();
}
