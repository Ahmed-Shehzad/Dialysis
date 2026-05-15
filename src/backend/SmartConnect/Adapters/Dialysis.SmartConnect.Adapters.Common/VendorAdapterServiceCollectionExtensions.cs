using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect.Adapters;

public static class VendorAdapterServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared <see cref="OAuth2TokenAcquirer"/>. Falls back to a singleton in-memory
    /// <see cref="IDistributedCache"/> if no implementation is already registered, so callers wiring
    /// vendor adapters in isolation (unit tests, dev hosts without Valkey) still get a working cache.
    /// Production hosts should register Valkey first via
    /// <c>services.AddValkeyDistributedCache(...)</c> — this method then reuses it.
    /// </summary>
    public static IServiceCollection AddVendorAdapterTokenAcquirer(this IServiceCollection services)
    {
        services.TryAddSingleton<IDistributedCache>(_ =>
            new MemoryDistributedCache(
                Microsoft.Extensions.Options.Options.Create(new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions())));
        services.TryAddSingleton<OAuth2TokenAcquirer>(sp => new OAuth2TokenAcquirer(
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<IDistributedCache>()));
        return services;
    }
}
