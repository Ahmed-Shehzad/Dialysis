using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Valkey.Glide;

namespace Dialysis.BuildingBlocks.DistributedCache.Valkey;

public static class ValkeyDistributedCacheServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Wires Valkey (via the Valkey GLIDE client) as the module's <see cref="IDistributedCache"/>,
        /// registers a shared <see cref="IConnectionMultiplexer"/>, persists ASP.NET Core Data Protection
        /// keys to Valkey, and registers a health check. Idempotent across multiple invocations within a host.
        /// </summary>
        /// <param name="configurationSection">Bind target for <see cref="ValkeyDistributedCacheOptions"/>.</param>
        /// <param name="healthCheckName">Health-check name (defaults to <c>valkey</c>); shows up in the readiness payload.</param>
        public IServiceCollection AddValkeyDistributedCache(
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

            // One shared GLIDE connection per host. The compat surface mirrors StackExchange.Redis
            // (ConnectionMultiplexer.Connect + IDatabase), so existing connection strings still apply.
            services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(resolved.ConnectionString));

            var instanceName = resolved.InstanceName;
            services.TryAddSingleton<IDistributedCache>(sp =>
                new GlideDistributedCache(sp.GetRequiredService<IConnectionMultiplexer>(), instanceName));

            if (resolved.UseDataProtectionKeyRing)
            {
                var keyRingKey = $"{resolved.InstanceName}:data-protection-keys";
                services.AddDataProtection().SetApplicationName(resolved.InstanceName);
                // Point Data Protection's XmlRepository at Valkey via the GLIDE-backed repository.
                // Done through IConfigureOptions so we can resolve the shared connection from DI.
                services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(sp =>
                    new ConfigureOptions<KeyManagementOptions>(o =>
                        o.XmlRepository = new GlideXmlRepository(
                            sp.GetRequiredService<IConnectionMultiplexer>(), keyRingKey)));
            }

            services.AddHealthChecks().AddCheck<GlideValkeyHealthCheck>(
                healthCheckName,
                failureStatus: HealthStatus.Unhealthy,
                tags: ["readiness", "cache"]);

            return services;
        }
    }
}
