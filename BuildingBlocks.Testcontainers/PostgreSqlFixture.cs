using Testcontainers.PostgreSql;

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
            ConnectionString = _container.GetConnectionString();
            return;
        }

        await InitLock.WaitAsync();
        try
        {
            if (_container is not null)
            {
                ConnectionString = _container.GetConnectionString();
                return;
            }

            _container = new PostgreSqlBuilder("postgres:16-alpine")
                .Build();

#pragma warning disable IDE0058
            await _container.StartAsync();
#pragma warning restore IDE0058
            ConnectionString = _container.GetConnectionString();
        }
        finally
        {
            InitLock.Release();
        }
    }
}
