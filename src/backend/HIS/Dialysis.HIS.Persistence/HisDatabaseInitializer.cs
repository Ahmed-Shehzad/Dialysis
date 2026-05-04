using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIS.Persistence;

/// <summary>
/// Ensures the HIS database model exists: <see cref="DbContext.Database.MigrateAsync"/> for SQL Server (see <c>Migrations/</c>),
/// <see cref="DbContext.Database.EnsureCreatedAsync"/> for other providers (for example the default in-memory database).
/// </summary>
internal sealed class HisDatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<HisDatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();
            if (string.Equals(db.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
                await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            else
                await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

            await HisDataSeeder.EnsureSchedulingResourcesAsync(db, logger, cancellationToken).ConfigureAwait(false);
            await HisDataSeeder.EnsureRaCapabilitySamplesAsync(db, logger, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HIS database initialization failed.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
