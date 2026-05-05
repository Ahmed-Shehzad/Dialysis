using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore.SqlServer;

public static class TransponderPersistenceHostExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations for <see cref="TransponderPersistenceDbContext"/> using the configured schema.
    /// </summary>
    public async static Task ApplyTransponderSqlServerPersistenceMigrationsAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransponderPersistenceDbContext>();
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
