using Testcontainers.PostgreSql;
using Npgsql;

using Xunit;

namespace BuildingBlocks.Testcontainers;

/// <summary>
/// Shared PostgreSQL container for integration tests. Uses a single container per test process.
/// Tests that need a real PostgreSQL database use this via <see cref="ICollectionFixture{PostgreSqlFixture}"/>.
/// Implements IAsyncLifetime for initialization and IAsyncDisposable for cleanup when the collection finishes.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private static PostgreSqlContainer? _container;
    private static readonly SemaphoreSlim InitLock = new(1, 1);

    /// <summary>
    /// Connection string for Npgsql / EF Core. Use with UseNpgsql(connectionString).
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task DisposeAsync()
    {
        PostgreSqlContainer? container = Interlocked.Exchange(ref _container, null);
        if (container is not null) await container.DisposeAsync().ConfigureAwait(false);
        InitLock.Dispose();
    }

    public async Task InitializeAsync()
    {
        if (_container is not null)
        {
            ConnectionString = await CreateIsolatedDatabaseConnectionStringAsync(_container).ConfigureAwait(false);
            return;
        }

        await InitLock.WaitAsync();
        try
        {
            if (_container is not null)
            {
                ConnectionString = await CreateIsolatedDatabaseConnectionStringAsync(_container).ConfigureAwait(false);
                return;
            }

            _container = new PostgreSqlBuilder("postgres:16-alpine")
                .Build();

#pragma warning disable IDE0058
            await _container.StartAsync();
#pragma warning restore IDE0058
            ConnectionString = await CreateIsolatedDatabaseConnectionStringAsync(_container).ConfigureAwait(false);
        }
        finally
        {
            InitLock.Release();
        }
    }

    private static async Task<string> CreateIsolatedDatabaseConnectionStringAsync(PostgreSqlContainer container)
    {
        string baseConnectionString = container.GetConnectionString();
        string databaseName = $"test_{Guid.NewGuid():N}";

        var adminBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = "postgres"
        };

        await using var adminConnection = new NpgsqlConnection(adminBuilder.ConnectionString);
        await adminConnection.OpenAsync().ConfigureAwait(false);

        await using var createDbCommand = adminConnection.CreateCommand();
        createDbCommand.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        _ = await createDbCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

        var isolatedBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = databaseName
        };
        return isolatedBuilder.ConnectionString;
    }
}
