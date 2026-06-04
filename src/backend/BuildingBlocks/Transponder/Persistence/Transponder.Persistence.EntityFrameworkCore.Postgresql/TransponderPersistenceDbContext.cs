using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore.Postgresql;

/// <summary>
/// PostgreSQL <see cref="DbContext"/> for Transponder persistence. Migrations live in this assembly under <c>Migrations/</c>.
/// </summary>
public sealed class TransponderPersistenceDbContext : TransponderPersistenceDbContextBase
{
    /// <summary>
    /// PostgreSQL <see cref="DbContext"/> for Transponder persistence. Migrations live in this assembly under <c>Migrations/</c>.
    /// </summary>
    public TransponderPersistenceDbContext(DbContextOptions<TransponderPersistenceDbContext> options,
        IOptions<TransponderPersistenceOptions> persistenceOptions) : base(options, persistenceOptions)
    {
    }
}
