namespace Dialysis.SmartConnect.Persistence.ObjectStorage.AzureBlob;

/// <summary>
/// Options for <see cref="AzureBlobAttachmentBlobStore"/>. Two authentication shapes are supported:
/// a full <see cref="ConnectionString"/> (Azurite, account-key dev/test) or a <see cref="ServiceUri"/>
/// pointing at a real Azure Storage account that the host accesses via <c>DefaultAzureCredential</c>
/// (managed identity, env vars, CLI). Production deployments should leave <see cref="ConnectionString"/>
/// unset and rely on managed identity.
/// </summary>
/// <remarks>
/// Mutable get/set because the <see cref="Microsoft.Extensions.Options"/> pattern instantiates the
/// options parameterlessly and applies configuration callbacks afterward. The store constructor
/// validates that <see cref="ContainerName"/> is non-empty and that exactly one of
/// <see cref="ConnectionString"/> / <see cref="ServiceUri"/> is provided.
/// </remarks>
public sealed class AzureBlobAttachmentBlobOptions
{
    /// <summary>Blob container the attachments are stored in. Auto-created on first write (idempotent).</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Full Azure Storage connection string. Use for Azurite / account-key dev setups. Mutually
    /// exclusive with <see cref="ServiceUri"/>. Example:
    /// <c>DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=...;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1</c>.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Blob service endpoint (e.g. <c>https://my-account.blob.core.windows.net</c>). When set, the
    /// store authenticates via <c>DefaultAzureCredential</c> — managed identity in Azure, environment
    /// or CLI credentials locally. Mutually exclusive with <see cref="ConnectionString"/>.
    /// </summary>
    public Uri? ServiceUri { get; set; }

    /// <summary>
    /// Prefix prepended to every blob name. Useful for sharing a container across modules
    /// (e.g. <c>smartconnect/attachments/</c>). Trailing slash optional — the store normalises it.
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;
}
