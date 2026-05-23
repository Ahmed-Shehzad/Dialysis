using Dialysis.SmartConnect.Attachments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect.Persistence.ObjectStorage.ContentAddressable;

/// <summary>
/// Composition helpers for the content-addressable storage decorator.
/// </summary>
public static class ContentAddressableAttachmentBlobStoreServiceCollectionExtensions
{
    /// <summary>
    /// Wraps the currently registered <see cref="IAttachmentBlobStore"/> with
    /// <see cref="ContentAddressableAttachmentBlobStore"/>. Call AFTER one of the concrete-store
    /// registrations (<c>UseFileSystemAttachmentBlobStore</c>, <c>UseS3AttachmentBlobStore</c>,
    /// <c>UseAzureBlobAttachmentBlobStore</c>) so this decorator can capture the inner store.
    /// </summary>
    public static IServiceCollection UseContentAddressableAttachmentBlobStore(this IServiceCollection services)
    {
        // Snapshot the existing IAttachmentBlobStore registration, then swap in the decorator.
        var existing = services.LastOrDefault(s => s.ServiceType == typeof(IAttachmentBlobStore))
            ?? throw new InvalidOperationException(
                "No IAttachmentBlobStore is registered. Call UseFileSystem/UseS3/UseAzureBlob first.");
        services.RemoveAll<IAttachmentBlobStore>();

        // Re-register the existing implementation under a marker type so the decorator can
        // resolve it without recursing into itself.
        services.Add(new ServiceDescriptor(typeof(InnerBlobStore), existing.ImplementationType
            ?? throw new InvalidOperationException("Existing IAttachmentBlobStore must have an implementation type."),
            existing.Lifetime));

        services.AddSingleton<IAttachmentBlobStore>(sp =>
        {
            var inner = (IAttachmentBlobStore)sp.GetRequiredService(typeof(InnerBlobStore));
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            return new ContentAddressableAttachmentBlobStore(inner, scopeFactory);
        });

        services.AddAttachmentOrphanReaper();
        return services;
    }

    /// <summary>
    /// Marker type used to re-register the wrapped <see cref="IAttachmentBlobStore"/> without
    /// the CAS decorator recursing into itself. Kept internal to the composition extension.
    /// </summary>
    private sealed class InnerBlobStore;
}
