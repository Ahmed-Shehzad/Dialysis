using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore.SqlServer;

public static class TransponderPersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Binds <see cref="TransponderPersistenceOptions"/> from <paramref name="configuration"/> (default section <c>Transponder:Persistence</c>),
    /// registers <see cref="TransponderPersistenceDbContext"/>, and stores EF history in <see cref="TransponderPersistenceOptions.Schema"/>.
    /// Pragmatic connection resolution: <see cref="TransponderPersistenceOptions.ConnectionString"/> if set, otherwise
    /// <see cref="Microsoft.Extensions.Configuration.IConfiguration.GetConnectionString(string)"/> using <see cref="TransponderPersistenceOptions.ConnectionStringName"/>.
    /// </summary>
    public static IServiceCollection AddTransponderSqlServerPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Transponder:Persistence")
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services
            .AddOptions<TransponderPersistenceOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.Schema),
                $"{sectionName}:Schema must be set (client-provided database schema for Transponder tables and migrations history).");

        RegisterSqlServerDbContext(services);
        return services;
    }

    /// <summary>
    /// Registers options from <paramref name="configure"/> then <see cref="TransponderPersistenceDbContext"/>.
    /// Use when you are not binding from <see cref="IConfiguration"/>.
    /// </summary>
    public static IServiceCollection AddTransponderSqlServerPersistence(
        this IServiceCollection services,
        Action<TransponderPersistenceOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.AddOptions<TransponderPersistenceOptions>()
            .Configure(configure)
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.Schema),
                "TransponderPersistenceOptions.Schema must be set.");

        RegisterSqlServerDbContext(services);
        return services;
    }

    /// <summary>
    /// Registers fixed connection string and schema (convenience when configuration is assembled in code).
    /// </summary>
    public static IServiceCollection AddTransponderSqlServerPersistence(
        this IServiceCollection services,
        string connectionString,
        string schema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        services.AddOptions<TransponderPersistenceOptions>()
            .Configure(o =>
            {
                o.ConnectionString = connectionString;
                o.Schema = schema;
            });
        RegisterSqlServerDbContext(services);
        return services;
    }

    private static void RegisterSqlServerDbContext(IServiceCollection services)
    {
        services.AddDbContext<TransponderPersistenceDbContext>((sp, ob) =>
        {
            var o = sp.GetRequiredService<IOptions<TransponderPersistenceOptions>>().Value;
            var cs = TransponderPersistenceConfiguration.ResolveConnectionString(o, sp.GetService<IConfiguration>());
            ob.UseSqlServer(
                cs,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", o.Schema));
        });
    }
}
