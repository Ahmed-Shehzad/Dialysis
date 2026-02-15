using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Transponder.Persistence.Abstractions;
using Transponder.Persistence.EntityFramework.Abstractions;

namespace Transponder.Persistence.EntityFramework.SqlServer;

/// <summary>
/// Extension methods to register SQL Server persistence services.
/// </summary>
public static class Extensions
{
    public static IServiceCollection AddTransponderSqlServerPersistence(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IEntityFrameworkDbContextFactory<SqlServerTransponderDbContext>,
            EntityFrameworkDbContextFactory<SqlServerTransponderDbContext>>();
        services.TryAddSingleton<IScheduledMessageStore,
            EntityFrameworkScheduledMessageStore<SqlServerTransponderDbContext>>();
        services.TryAddSingleton<IStorageSessionFactory,
            EntityFrameworkStorageSessionFactory<SqlServerTransponderDbContext>>();

        return services;
    }
}
