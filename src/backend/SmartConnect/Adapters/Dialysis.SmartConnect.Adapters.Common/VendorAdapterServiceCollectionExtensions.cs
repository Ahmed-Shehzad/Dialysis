using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace Dialysis.SmartConnect.Adapters;

public static class VendorAdapterServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the shared <see cref="OAuth2TokenAcquirer"/>. Falls back to a singleton in-memory
        /// <see cref="IDistributedCache"/> if no implementation is already registered, so callers wiring
        /// vendor adapters in isolation (unit tests, dev hosts without Valkey) still get a working cache.
        /// Production hosts should register Valkey first via
        /// <c>services.AddValkeyDistributedCache(...)</c> — this method then reuses it.
        /// </summary>
        public IServiceCollection AddVendorAdapterTokenAcquirer()
        {
            services.TryAddSingleton<IDistributedCache>(_ =>
                new MemoryDistributedCache(
                    Options.Create(new MemoryDistributedCacheOptions())));
            services.TryAddSingleton<OAuth2TokenAcquirer>(sp => new OAuth2TokenAcquirer(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<IDistributedCache>()));
            return services;
        }

        /// <summary>
        /// Registers the named vendor <see cref="HttpClient"/> with a Polly transient-error retry
        /// policy (3 attempts, exponential backoff). External EHR endpoints (Epic / Cerner /
        /// Allscripts / Meditech / OpenEMR) sit across the public internet — a blip must not
        /// surface as a failed adapter call when a retry would have succeeded.
        /// </summary>
        public IHttpClientBuilder AddResilientVendorHttpClient(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            return services.AddHttpClient(name)
                .AddPolicyHandler(HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(retryCount: 3, sleepDurationProvider: static attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))));
        }
    }
}
