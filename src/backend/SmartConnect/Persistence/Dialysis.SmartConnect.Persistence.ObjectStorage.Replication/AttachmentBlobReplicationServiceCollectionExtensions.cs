using Dialysis.SmartConnect.Attachments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Dialysis.SmartConnect.Persistence.ObjectStorage.Replication;

/// <summary>
/// Composition helpers for cross-region replication.
/// </summary>
public static class AttachmentBlobReplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="NoOpReplicationStrategy"/> as the default. Host wiring multi-region
    /// substitutes via <see cref="UseMultiRegionAttachmentReplication"/>.
    /// </summary>
    public static IServiceCollection AddDefaultAttachmentReplication(this IServiceCollection services)
    {
        services.TryAddSingleton<IAttachmentBlobReplicationStrategy, NoOpReplicationStrategy>();
        return services;
    }

    /// <summary>
    /// Replaces the registered strategy with <see cref="MultiRegionReplicationStrategy"/>. Pass a
    /// factory that resolves the secondary stores from the service provider — typically each
    /// secondary is registered via its own keyed registration (per-region S3/AzureBlob client).
    /// </summary>
    public static IServiceCollection UseMultiRegionAttachmentReplication(
        this IServiceCollection services,
        ReplicationMode mode,
        Func<IServiceProvider, IReadOnlyList<IAttachmentBlobStore>> secondariesFactory)
    {
        ArgumentNullException.ThrowIfNull(secondariesFactory);
        services.RemoveAll<IAttachmentBlobReplicationStrategy>();
        services.AddSingleton<IAttachmentBlobReplicationStrategy>(sp =>
            new MultiRegionReplicationStrategy(
                mode,
                secondariesFactory(sp),
                sp.GetRequiredService<ILogger<MultiRegionReplicationStrategy>>()));
        return services;
    }
}
