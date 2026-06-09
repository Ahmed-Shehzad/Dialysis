using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Dialysis.EHR.Tests;

/// <summary>
/// Shared PostgreSQL Testcontainer for EHR unit tests that need a real database (replacing the EF
/// in-memory provider). One container is started for the whole assembly and each call yields a fresh,
/// isolated database; the container is reaped by Testcontainers at process exit.
/// </summary>
public static class EhrTestPostgres
{
    private static readonly Lock _syncRoot = new();
    private static PostgreSqlContainer? _shared;

    /// <summary>A connection string for a fresh, isolated database on the shared container.</summary>
    public static string NewDatabaseConnectionString() =>
        new NpgsqlConnectionStringBuilder(Container().GetConnectionString())
        {
            Database = $"ehr_test_{Guid.NewGuid():N}",
            // Each test uses its own database, so disable pooling to avoid accumulating idle connections.
            Pooling = false,
        }.ConnectionString;

    /// <summary>Creates the database + schema for <paramref name="database"/>, serialized (CREATE DATABASE can't run concurrently).</summary>
    public static void EnsureCreated(DatabaseFacade database)
    {
        lock (_syncRoot)
        {
            database.EnsureCreated();
        }
    }

    private static PostgreSqlContainer Container()
    {
        if (_shared is not null)
            return _shared;
        lock (_syncRoot)
        {
            if (_shared is null)
            {
                var container = new PostgreSqlBuilder("postgres:17-alpine").Build();
                // Intentional synchronous block — these are plain unit tests with no async fixture hook.
#pragma warning disable VSTHRD002
                container.StartAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
                _shared = container;
            }
            return _shared;
        }
    }
}
