using System.Data.Common;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Resolves a <see cref="DbConnection"/> for a given provider + connection-string name.
/// Implementations control how the actual connection string is fetched (configuration, secret store, ...)
/// and how the <see cref="DbProviderFactory"/> is selected.
/// </summary>
public interface IDatabaseOutboundConnectionFactory
{
    /// <summary>
    /// Open a connection for <paramref name="provider"/> using the connection string named
    /// <paramref name="connectionStringName"/>. Throws when not configured.
    /// </summary>
    Task<DbConnection> OpenAsync(
        DatabaseProvider provider,
        string connectionStringName,
        CancellationToken cancellationToken);
}
