using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Shared PostgreSQL Testcontainer for the SmartConnect test assembly. Replaces the EF in-memory
/// provider: one container is started for the whole assembly and each test gets its own freshly-created
/// database (full schema via <see cref="DatabaseFacade.EnsureCreated"/>) so tests stay isolated. The
/// container is reaped by Testcontainers at process exit.
/// </summary>
public static class SmartConnectPostgres
{
    private static readonly Lock _syncRoot = new();
    private static PostgreSqlContainer? _shared;

    /// <summary>
    /// Creates a fresh, isolated database on the shared container, applies the SmartConnect schema, and
    /// returns its connection string. Pass the result to <c>AddSmartConnectPersistenceForPostgresql</c>.
    /// </summary>
    public static string NewDatabaseConnectionString()
    {
        var databaseName = $"sc_{Guid.NewGuid():N}";
        // CREATE DATABASE can't run concurrently against the same template, so serialize creation +
        // schema build across xUnit's parallel test classes.
        lock (_syncRoot)
        {
            var connectionString = new NpgsqlConnectionStringBuilder(Container().GetConnectionString())
            {
                Database = databaseName,
                // Each test gets its own database, so hundreds of distinct connection pools would otherwise
                // accumulate idle connections and exhaust the shared server ("too many clients"). Disable
                // pooling so only actively-used connections exist at any moment.
                Pooling = false,
            }.ConnectionString;

            var options = new DbContextOptionsBuilder<SmartConnectDbContext>()
                .UseNpgsql(connectionString)
                .Options;
            using var context = new SmartConnectDbContext(options);
            context.Database.EnsureCreated();
            return connectionString;
        }
    }

    private static PostgreSqlContainer Container()
    {
        if (_shared is not null)
            return _shared;
        // Started synchronously inside the creation lock (tests build service providers eagerly with no
        // async hook). Intentional block — see HieWebApplicationFactory for the same constraint.
        // Raise max_connections well above the default 100: the suite runs many databases concurrently
        // under xUnit parallelism, each with its own (small) pool.
        var container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithCommand("-c", "max_connections=300")
            .Build();
#pragma warning disable VSTHRD002
        container.StartAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        _shared = container;
        return _shared;
    }
}
