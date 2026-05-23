using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Composition helpers for out-of-row attachment backends. Hosts call <c>AddSmartConnectPersistence</c>
/// first (which registers the default <c>InRowAttachmentBlobStore</c>), then call one of these methods
/// to replace the blob store with a filesystem / S3 / Azure Blob impl and opt into the orphan reaper.
/// </summary>
public static class AttachmentBlobStoreServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the registered <see cref="IAttachmentBlobStore"/> with <see cref="FileSystemAttachmentBlobStore"/>
    /// and registers <see cref="AttachmentOrphanReaperHostedService"/>. Call after <c>AddSmartConnectPersistence</c>.
    /// </summary>
    public static IServiceCollection UseFileSystemAttachmentBlobStore(
        this IServiceCollection services,
        Action<FileSystemAttachmentBlobOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        services.RemoveAll<IAttachmentBlobStore>();
        services.AddScoped<IAttachmentBlobStore, FileSystemAttachmentBlobStore>();
        services.AddAttachmentOrphanReaper();
        return services;
    }

    /// <summary>
    /// Registers the periodic orphan reaper. The reaper itself checks <see cref="IAttachmentBlobStore.StoresBytesInRow"/>
    /// at runtime and exits early when the registered backend is in-row, so a default registration is
    /// harmless. <see cref="TimeProvider.System"/> is registered if no instance is already wired.
    /// </summary>
    public static IServiceCollection AddAttachmentOrphanReaper(
        this IServiceCollection services,
        Action<AttachmentOrphanReaperOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<AttachmentOrphanReaperOptions>();
        }
        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<AttachmentOrphanReaperHostedService>();
        return services;
    }

    /// <summary>
    /// Registers the default no-op <see cref="IAttachmentBlobScanner"/> (always-clean). Hosts wiring
    /// a real scanner call <c>UseClamAvAttachmentBlobScanner</c> after this; the AV-adapter package
    /// removes this registration and substitutes its own.
    /// </summary>
    public static IServiceCollection AddNullAttachmentBlobScanner(this IServiceCollection services)
    {
        services.TryAddSingleton<IAttachmentBlobScanner, NullAttachmentBlobScanner>();
        return services;
    }

    /// <summary>
    /// Registers the default <see cref="NullAttachmentDownloadUrlFactory"/>. Per-store packages
    /// (S3, AzureBlob) call <c>Use…AttachmentDownloadUrls</c> to substitute their signing impl.
    /// </summary>
    public static IServiceCollection AddNullAttachmentDownloadUrlFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<IAttachmentDownloadUrlFactory, NullAttachmentDownloadUrlFactory>();
        return services;
    }
}
