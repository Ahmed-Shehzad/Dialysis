using System.Runtime.CompilerServices;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Dialysis.SmartConnect.Attachments;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Persistence.ObjectStorage.AzureBlob;

/// <summary>
/// Out-of-row <see cref="IAttachmentBlobStore"/> backed by Azure Blob Storage (or any
/// Azurite-compatible emulator). Pairs with the orphan reaper because metadata-save failures after
/// the blob lands would otherwise leave stranded objects in the container forever.
/// </summary>
/// <remarks>
/// Unlike the S3 SDK, Azure.Storage.Blobs exposes proper synchronous APIs, so the Jint sync-write
/// binding path remains valid against this backend.
/// </remarks>
public sealed class AzureBlobAttachmentBlobStore : IAttachmentBlobStore
{
    private readonly BlobContainerClient _container;
    private readonly string _prefix;

    public AzureBlobAttachmentBlobStore(IOptions<AzureBlobAttachmentBlobOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.ContainerName))
        {
            throw new ArgumentException("ContainerName must be configured.", nameof(options));
        }
        var hasConn = !string.IsNullOrWhiteSpace(opts.ConnectionString);
        var hasUri = opts.ServiceUri is not null;
        if (hasConn == hasUri)
        {
            throw new ArgumentException(
                "Exactly one of ConnectionString or ServiceUri must be set.", nameof(options));
        }

        _prefix = NormalisePrefix(opts.KeyPrefix);
        _container = BuildContainerClient(opts);
        _container.CreateIfNotExists();
    }

    public bool StoresBytesInRow => false;

    public async Task WriteAsync(Guid attachmentId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(data.ToArray(), writable: false);
        var blob = _container.GetBlobClient(KeyOf(attachmentId));
        await blob.UploadAsync(stream, overwrite: true, cancellationToken).ConfigureAwait(false);
    }

    public void Write(Guid attachmentId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new MemoryStream(data.ToArray(), writable: false);
        var blob = _container.GetBlobClient(KeyOf(attachmentId));
        blob.Upload(stream, overwrite: true, cancellationToken);
    }

    public async Task<ReadOnlyMemory<byte>?> ReadAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        var blob = _container.GetBlobClient(KeyOf(attachmentId));
        try
        {
            var response = await blob.DownloadContentAsync(cancellationToken).ConfigureAwait(false);
            return new ReadOnlyMemory<byte>(response.Value.Content.ToArray());
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        var blob = _container.GetBlobClient(KeyOf(attachmentId));
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<BlobMetadata> EnumerateAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in _container.GetBlobsAsync(prefix: _prefix, cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            var name = item.Name;
            if (string.IsNullOrEmpty(name)) continue;
            var trimmed = name.StartsWith(_prefix, StringComparison.Ordinal)
                ? name[_prefix.Length..]
                : name;
            if (!Guid.TryParse(trimmed, out var id)) continue;
            var created = item.Properties.CreatedOn ?? item.Properties.LastModified ?? DateTimeOffset.UtcNow;
            yield return new BlobMetadata(id, created, item.Properties.ContentLength ?? 0);
        }
    }

    private string KeyOf(Guid id) => _prefix + id.ToString("D");

    private static string NormalisePrefix(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        return raw.EndsWith('/') ? raw : raw + "/";
    }

    private static BlobContainerClient BuildContainerClient(AzureBlobAttachmentBlobOptions opts)
    {
        if (!string.IsNullOrWhiteSpace(opts.ConnectionString))
        {
            return new BlobContainerClient(opts.ConnectionString, opts.ContainerName);
        }
        var serviceClient = new BlobServiceClient(opts.ServiceUri, new DefaultAzureCredential());
        return serviceClient.GetBlobContainerClient(opts.ContainerName);
    }
}
