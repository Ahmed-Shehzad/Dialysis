using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql;

/// <summary>PostgreSQL persistence plugin for SmartConnect.</summary>
public static class SmartConnectPostgresqlPersistenceExtensions
{
    private static readonly string MigrationsAssemblyName =
        typeof(SmartConnectPostgresqlPersistenceExtensions).Assembly.GetName().Name!;

    /// <summary>Registers SmartConnect persistence against PostgreSQL, with migrations in this plugin assembly.</summary>
    public static IServiceCollection AddSmartConnectPersistenceForPostgresql(
        this IServiceCollection services,
        string connectionString) =>
        services.AddSmartConnectPersistence(o => o.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "smartconnect");
                npgsql.MigrationsAssembly(MigrationsAssemblyName);
            }));
}
