using Dialysis.SmartConnect.Attachments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect.Persistence.ObjectStorage.S3;

/// <summary>
/// Composition helpers for the S3-compatible attachment backend. Call after
/// <c>AddSmartConnectPersistence</c> (which registers the default in-row store) to swap in S3
/// and opt into the orphan reaper.
/// </summary>
public static class S3AttachmentBlobStoreServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the registered <see cref="IAttachmentBlobStore"/> with <see cref="S3AttachmentBlobStore"/>
    /// and registers <see cref="AttachmentOrphanReaperHostedService"/>. The S3 client is a singleton —
    /// the SDK reuses HTTP connections across calls and is documented as thread-safe.
    /// </summary>
    public static IServiceCollection UseS3AttachmentBlobStore(
        this IServiceCollection services,
        Action<S3AttachmentBlobOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        services.RemoveAll<IAttachmentBlobStore>();
        services.AddSingleton<S3AttachmentBlobStore>();
        services.AddSingleton<IAttachmentBlobStore>(sp => sp.GetRequiredService<S3AttachmentBlobStore>());
        services.AddAttachmentOrphanReaper();
        return services;
    }
}
