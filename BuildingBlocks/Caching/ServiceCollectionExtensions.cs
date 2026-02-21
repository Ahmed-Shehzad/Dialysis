using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Caching;

/// <summary>
/// Registers Read-Through and cache invalidation (Write-Through) services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Read-Through and invalidation using IDistributedCache from the service provider.
    /// Call after AddStackExchangeRedisCache (or similar) when Redis is configured.
    /// </summary>
    public static IServiceCollection AddReadThroughCache(this IServiceCollection services)
    {
        return services
            .AddSingleton<IReadThroughCache>(sp =>
                new ReadThroughDistributedCache(sp.GetRequiredService<IDistributedCache>()))
            .AddSingleton<ICacheInvalidator>(sp =>
                new DistributedCacheInvalidator(sp.GetRequiredService<IDistributedCache>()));
    }

    /// <summary>
    /// Adds no-op Read-Through and invalidation when Redis is not configured.
    /// </summary>
    public static IServiceCollection AddNullReadThroughCache(this IServiceCollection services)
    {
        return services
            .AddSingleton<IReadThroughCache, NullReadThroughCache>()
            .AddSingleton<ICacheInvalidator, NullCacheInvalidator>();
    }
}
