using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Dialysis.BuildingBlocks.DistributedCache.Valkey;

public static class ValkeyDistributedCacheServiceCollectionExtensions
{
    /// <summary>
    /// Wires Valkey (Redis-protocol) as the module's <see cref="IDistributedCache"/>, registers a
    /// shared <see cref="IConnectionMultiplexer"/>, persists ASP.NET Core Data Protection keys to
    /// Valkey, and registers a health check. Idempotent across multiple invocations within a host.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configurationSection">Bind target for <see cref="ValkeyDistributedCacheOptions"/>.</param>
    /// <param name="healthCheckName">Health-check name (defaults to <c>valkey</c>); shows up in the readiness payload.</param>
    public static IServiceCollection AddValkeyDistributedCache(
        this IServiceCollection services,
        IConfiguration configurationSection,
        string healthCheckName = "valkey")
    {
        services.AddOptions<ValkeyDistributedCacheOptions>().Bind(configurationSection);

        var resolved = configurationSection.Get<ValkeyDistributedCacheOptions>();
        if (resolved is null || string.IsNullOrWhiteSpace(resolved.ConnectionString))
        {
            // Configuration not present at startup — register a fallback in-memory cache so module hosts
            // can boot in dev without Valkey. Production hosts should validate the config explicitly.
            services.AddDistributedMemoryCache();
            services.TryAddSingleton<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()));
            return services;
        }

        services.AddStackExchangeRedisCache(o =>
        {
            o.Configuration = resolved.ConnectionString;
            o.InstanceName = resolved.InstanceName + ":";
        });

        services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(resolved.ConnectionString));

        if (resolved.UseDataProtectionKeyRing)
        {
            var keyName = $"{resolved.InstanceName}:data-protection-keys";
            var dpMultiplexer = new Lazy<IConnectionMultiplexer>(
                () => ConnectionMultiplexer.Connect(resolved.ConnectionString),
                LazyThreadSafetyMode.ExecutionAndPublication);
            services
                .AddDataProtection()
                .SetApplicationName(resolved.InstanceName)
                .PersistKeysToStackExchangeRedis(() => dpMultiplexer.Value.GetDatabase(), keyName);
        }

        services.AddHealthChecks().AddRedis(
            sp => sp.GetRequiredService<IConnectionMultiplexer>(),
            name: healthCheckName,
            failureStatus: HealthStatus.Unhealthy,
            tags: ["readiness", "cache"]);

        return services;
    }
}
