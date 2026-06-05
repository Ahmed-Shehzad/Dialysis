using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Documents.Storage.Valkey;

/// <summary>DI registration for the Valkey-backed <see cref="IDocumentBlobStore"/>.</summary>
public static class ValkeyDocumentBlobStoreServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Replaces the registered <see cref="IDocumentBlobStore"/> with <see cref="ValkeyDocumentBlobStore"/>
        /// when a connection string is present in <paramref name="valkeySection"/> (the same
        /// <c>&lt;Module&gt;:DistributedCache:Valkey</c> section the cache binds). The store resolves the
        /// shared <see cref="Valkey.Glide.IConnectionMultiplexer"/> that <c>AddValkeyDistributedCache</c>
        /// registers, so callers must register the cache first (module hosting does this before the
        /// composition extensions run).
        ///
        /// When Valkey is not configured this is a no-op — the caller's previously-registered
        /// in-memory / filesystem default stands, so dev without Valkey and tests keep working.
        /// </summary>
        public IServiceCollection AddValkeyDocumentBlobStore(IConfiguration valkeySection)
        {
            ArgumentNullException.ThrowIfNull(valkeySection);
            if (string.IsNullOrWhiteSpace(valkeySection["ConnectionString"]))
                return services;

            services.RemoveAll<IDocumentBlobStore>();
            services.AddSingleton<IDocumentBlobStore, ValkeyDocumentBlobStore>();
            return services;
        }
    }
}
