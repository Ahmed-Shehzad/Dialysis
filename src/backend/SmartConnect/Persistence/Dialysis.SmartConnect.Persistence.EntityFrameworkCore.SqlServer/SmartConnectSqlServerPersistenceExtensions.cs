using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.SqlServer;

/// <summary>SQL Server persistence plugin for SmartConnect.</summary>
public static class SmartConnectSqlServerPersistenceExtensions
{
    private static readonly string MigrationsAssemblyName =
        typeof(SmartConnectSqlServerPersistenceExtensions).Assembly.GetName().Name!;

    /// <summary>Registers SmartConnect persistence against SQL Server, with migrations in this plugin assembly.</summary>
    public static IServiceCollection AddSmartConnectPersistenceForSqlServer(
        this IServiceCollection services,
        string connectionString) =>
        services.AddSmartConnectPersistence(o => o.UseSqlServer(
            connectionString,
            sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "smartconnect");
                sql.MigrationsAssembly(MigrationsAssemblyName);
            }));
}
