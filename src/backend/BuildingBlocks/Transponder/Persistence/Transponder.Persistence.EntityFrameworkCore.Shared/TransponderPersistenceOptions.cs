namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

/// <summary>
/// Client configuration for Transponder EF persistence (schema + connection resolution).
/// </summary>
public sealed class TransponderPersistenceOptions
{
    /// <summary>
    /// Database schema that owns Transponder tables and <c>__EFMigrationsHistory</c> (SQL Server) or equivalent (PostgreSQL).
    /// </summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// When set, used as the ADO.NET connection string. Otherwise <see cref="ConnectionStringName"/> is read from <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Passed to <see cref="Microsoft.Extensions.Configuration.IConfiguration.GetConnectionString(string)"/> when <see cref="ConnectionString"/> is empty. Default <c>TransponderPersistence</c>.
    /// </summary>
    public string ConnectionStringName { get; set; } = "TransponderPersistence";
}
