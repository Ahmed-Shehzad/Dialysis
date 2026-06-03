using Azure.Storage.Blobs;

namespace Dialysis.SmartConnect.Persistence.ObjectStorage.AzureBlob;

/// <summary>
/// Shared Azure Blob client configuration. The Storage REST API version is pinned because the SDK
/// default now tracks the newest service version (<c>2026-04-06</c>), which outpaces the Azurite
/// emulator used in tests. Pinning also keeps production behaviour deterministic across SDK bumps;
/// real Azure Storage is backward-compatible with this version, and it covers every operation this
/// backend uses (put / get / list / delete / SAS).
/// </summary>
internal static class AzureBlobClientDefaults
{
    private const BlobClientOptions.ServiceVersion PinnedServiceVersion =
        BlobClientOptions.ServiceVersion.V2025_05_05;

    public static BlobClientOptions CreateOptions() => new(PinnedServiceVersion);
}
