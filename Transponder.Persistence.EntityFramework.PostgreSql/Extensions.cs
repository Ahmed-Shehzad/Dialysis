using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Transponder.Persistence.Abstractions;
using Transponder.Persistence.EntityFramework.Abstractions;

namespace Transponder.Persistence.EntityFramework.PostgreSql;

/// <summary>
/// Extension methods to register PostgreSQL persistence services.
/// </summary>
public static class Extensions
{
    public static IServiceCollection AddTransponderPostgreSqlPersistence(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IEntityFrameworkDbContextFactory<PostgreSqlTransponderDbContext>,
            EntityFrameworkDbContextFactory<PostgreSqlTransponderDbContext>>();
        services.TryAddSingleton<IScheduledMessageStore,
            EntityFrameworkScheduledMessageStore<PostgreSqlTransponderDbContext>>();
        services.TryAddSingleton<IStorageSessionFactory,
            EntityFrameworkStorageSessionFactory<PostgreSqlTransponderDbContext>>();

        return services;
    }
}
