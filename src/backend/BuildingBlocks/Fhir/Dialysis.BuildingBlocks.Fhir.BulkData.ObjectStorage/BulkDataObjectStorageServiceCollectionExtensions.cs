using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Fhir.BulkData.ObjectStorage;

/// <summary>
/// Swaps the Bulk Data sink from the default local-file store to a cloud object store. Call after
/// <c>AddFhirBulkDataOrchestrator(...)</c> (which registers <c>LocalFileBulkDataStorage</c> via TryAdd);
/// these helpers <c>RemoveAll</c> that registration first, so the cloud sink wins.
/// </summary>
public static class BulkDataObjectStorageServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Routes Bulk Data NDJSON output to S3 / MinIO.</summary>
        public IServiceCollection UseS3BulkDataStorage(Action<S3BulkDataStorageOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            services.Configure(configure);
            services.RemoveAll<IBulkDataStorage>();
            services.AddSingleton<S3BulkDataStorage>();
            services.AddSingleton<IBulkDataStorage>(sp => sp.GetRequiredService<S3BulkDataStorage>());
            return services;
        }

        /// <summary>Routes Bulk Data NDJSON output to Azure Blob / Azurite.</summary>
        public IServiceCollection UseAzureBlobBulkDataStorage(Action<AzureBlobBulkDataStorageOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);
            services.Configure(configure);
            services.RemoveAll<IBulkDataStorage>();
            services.AddSingleton<AzureBlobBulkDataStorage>();
            services.AddSingleton<IBulkDataStorage>(sp => sp.GetRequiredService<AzureBlobBulkDataStorage>());
            return services;
        }
    }
}
