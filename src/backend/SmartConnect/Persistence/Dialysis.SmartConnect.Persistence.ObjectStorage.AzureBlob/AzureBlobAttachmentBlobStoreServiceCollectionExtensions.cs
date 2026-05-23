using Dialysis.SmartConnect.Attachments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect.Persistence.ObjectStorage.AzureBlob;

/// <summary>
/// Composition helpers for the Azure Blob attachment backend. Call after
/// <c>AddSmartConnectPersistence</c> (which registers the default in-row store) to swap in
/// Azure Blob Storage and opt into the orphan reaper.
/// </summary>
public static class AzureBlobAttachmentBlobStoreServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the registered <see cref="IAttachmentBlobStore"/> with
    /// <see cref="AzureBlobAttachmentBlobStore"/> and registers
    /// <c>AttachmentOrphanReaperHostedService</c>. The container client is a singleton — the Azure
    /// SDK reuses HTTP connections across calls and is documented as thread-safe.
    /// </summary>
    public static IServiceCollection UseAzureBlobAttachmentBlobStore(
        this IServiceCollection services,
        Action<AzureBlobAttachmentBlobOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        services.RemoveAll<IAttachmentBlobStore>();
        services.AddSingleton<AzureBlobAttachmentBlobStore>();
        services.AddSingleton<IAttachmentBlobStore>(sp => sp.GetRequiredService<AzureBlobAttachmentBlobStore>());
        services.AddAttachmentOrphanReaper();
        return services;
    }

    /// <summary>
    /// Registers the Azure Blob SAS URL factory as the active <see cref="IAttachmentDownloadUrlFactory"/>.
    /// Reuses the same options registered by <see cref="UseAzureBlobAttachmentBlobStore"/>; call after that.
    /// </summary>
    public static IServiceCollection UseAzureBlobSasAttachmentDownloadUrls(this IServiceCollection services)
    {
        services.RemoveAll<IAttachmentDownloadUrlFactory>();
        services.AddSingleton<IAttachmentDownloadUrlFactory, AzureBlobSasAttachmentDownloadUrlFactory>();
        return services;
    }
}
