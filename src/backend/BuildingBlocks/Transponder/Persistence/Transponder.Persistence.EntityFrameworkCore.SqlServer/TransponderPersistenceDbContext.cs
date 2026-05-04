using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore.SqlServer;

/// <summary>
/// SQL Server <see cref="DbContext"/> for Transponder persistence. Migrations live in this assembly under <c>Migrations/</c>.
/// </summary>
public sealed class TransponderPersistenceDbContext(
    DbContextOptions<TransponderPersistenceDbContext> options,
    IOptions<TransponderPersistenceOptions> persistenceOptions)
    : TransponderPersistenceDbContextBase(options, persistenceOptions);
