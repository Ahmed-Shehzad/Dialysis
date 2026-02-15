using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Dialysis.Tests;

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;

/// <summary>
/// Shared PostgreSQL container for integration tests. Shared across all tests in Postgres collection.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("postgres")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private bool _initialized;

    public string ConnectionString => _container.GetConnectionString();

    public string GetConnectionStringForDatabase(string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(ConnectionString) { Database = databaseName };
        return builder.ToString();
    }

    public Task InitializeAsync()
    {
        if (_initialized) return Task.CompletedTask;
        _initialized = true;
        return InitializeCoreAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private async Task InitializeCoreAsync()
    {
        await _container.StartAsync();
        await CreateTestDatabasesAsync();
    }

    private async Task CreateTestDatabasesAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        foreach (var db in new[] { "fhir_subscriptions", "dialysis_alerting_default", "dialysis_audit_default" })
        {
            await using var cmd = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = '{db}'", conn);
            var exists = await cmd.ExecuteScalarAsync();
            if (exists is null)
            {
                await using var create = new NpgsqlCommand($"CREATE DATABASE \"{db}\"", conn);
                await create.ExecuteNonQueryAsync();
            }
        }
    }
}
