using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql;

/// <summary>PostgreSQL persistence plugin for SmartConnect.</summary>
public static class SmartConnectPostgresqlPersistenceExtensions
{
    private static readonly string _migrationsAssemblyName =
        typeof(SmartConnectPostgresqlPersistenceExtensions).Assembly.GetName().Name!;

    extension(IServiceCollection services)
    {
        /// <summary>Registers SmartConnect persistence against PostgreSQL, with migrations in this plugin assembly.</summary>
        public IServiceCollection AddSmartConnectPersistenceForPostgresql(
            string connectionString) =>
            services.AddSmartConnectPersistence(o => o.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "smartconnect");
                    npgsql.MigrationsAssembly(_migrationsAssemblyName);
                }));
    }
}
