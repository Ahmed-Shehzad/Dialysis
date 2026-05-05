using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Default <see cref="IDatabaseOutboundConnectionFactory"/> backed by <see cref="IConfiguration"/>:
/// resolves the named connection string and returns a provider-specific connection.
/// </summary>
public sealed class ConfigurationDatabaseOutboundConnectionFactory(IConfiguration configuration)
    : IDatabaseOutboundConnectionFactory
{
    public async Task<DbConnection> OpenAsync(
        DatabaseProvider provider,
        string connectionStringName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);
        var connectionString = configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Database outbound: no connection string is configured under name '{connectionStringName}'.");
        }

        DbConnection connection = provider switch
        {
            DatabaseProvider.SqlServer => new SqlConnection(connectionString),
            DatabaseProvider.Postgres => new NpgsqlConnection(connectionString),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported database provider."),
        };

        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
