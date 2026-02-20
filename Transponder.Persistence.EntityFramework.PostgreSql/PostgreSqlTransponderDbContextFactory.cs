using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Transponder.Persistence.EntityFramework.PostgreSql;

/// <summary>
/// Design-time factory for creating PostgreSqlTransponderDbContext (e.g. for migrations).
/// </summary>
internal sealed class PostgreSqlTransponderDbContextFactory : IDesignTimeDbContextFactory<PostgreSqlTransponderDbContext>
{
    public PostgreSqlTransponderDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__TransponderDb")
            ?? "Host=localhost;Database=transponder;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<PostgreSqlTransponderDbContext>();
        _ = optionsBuilder.UseNpgsql(connectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

        var storageOptions = new PostgreSqlStorageOptions();
        return new PostgreSqlTransponderDbContext(optionsBuilder.Options, storageOptions);
    }
}
