using System.Net;
using System.Runtime.CompilerServices;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Dialysis.SmartConnect.Attachments;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Persistence.ObjectStorage.S3;

/// <summary>
/// Out-of-row <see cref="IAttachmentBlobStore"/> backed by any S3-compatible service (AWS S3, MinIO,
/// Wasabi, R2). Pairs with the orphan reaper because metadata-save failures after the blob lands
/// would otherwise leave stranded objects in the bucket forever.
/// </summary>
/// <remarks>
/// The sync <see cref="Write"/> path throws because the AWS SDK is async-only since v4. Hosts wiring
/// S3 must drive attachment storage through the async APIs; the Jint sync binding path is
/// incompatible with S3 by design.
/// </remarks>
public sealed class S3AttachmentBlobStore : IAttachmentBlobStore, IDisposable
{
    // AmazonS3Client (concrete) rather than IAmazonS3 to satisfy CA1859 — the interface
    // dispatch overhead isn't justified here since we always own the concrete client.
    private readonly AmazonS3Client _client;
    private readonly string _bucket;
    private readonly string _prefix;

    public S3AttachmentBlobStore(IOptions<S3AttachmentBlobOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.BucketName))
        {
            throw new ArgumentException("BucketName must be configured.", nameof(options));
        }
        _bucket = opts.BucketName;
        _prefix = NormalisePrefix(opts.KeyPrefix);
        _client = BuildClient(opts);
    }

    public bool StoresBytesInRow => false;

    public async Task WriteAsync(Guid attachmentId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(data.ToArray(), writable: false);
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = KeyOf(attachmentId),
            InputStream = stream,
            ContentType = "application/octet-stream",
            DisablePayloadSigning = true,
        };
        await _client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Not supported. The S3 SDK exposes no synchronous APIs since v4, and blocking on async here
    /// would deadlock under the ASP.NET sync context. The Jint sync-write binding is only valid
    /// when an in-row or filesystem store is registered.
    /// </summary>
    public void Write(Guid attachmentId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "S3AttachmentBlobStore does not support synchronous writes; call WriteAsync instead.");

    public async Task<ReadOnlyMemory<byte>?> ReadAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _client.GetObjectAsync(_bucket, KeyOf(attachmentId), cancellationToken)
                .ConfigureAwait(false);
            using var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            return new ReadOnlyMemory<byte>(buffer.ToArray());
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        await _client.DeleteObjectAsync(_bucket, KeyOf(attachmentId), cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<BlobMetadata> EnumerateAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string? continuationToken = null;
        do
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _bucket,
                Prefix = _prefix,
                ContinuationToken = continuationToken,
            };
            var response = await _client.ListObjectsV2Async(request, cancellationToken).ConfigureAwait(false);
            var objects = response.S3Objects;
            if (objects is null) continue;
            foreach (var obj in objects)
            {
                var key = obj.Key;
                if (string.IsNullOrEmpty(key)) continue;
                var name = key.StartsWith(_prefix, StringComparison.Ordinal)
                    ? key[_prefix.Length..]
                    : key;
                if (!Guid.TryParse(name, out var id)) continue;
                yield return new BlobMetadata(id, ToDateTimeOffset(obj.LastModified), obj.Size ?? 0);
            }
            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        }
        while (continuationToken is not null);
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    private string KeyOf(Guid id) => _prefix + id.ToString("D");

    // S3Object.LastModified is DateTime? in SDK v4 — convert to DateTimeOffset for BlobMetadata.
    private static DateTimeOffset ToDateTimeOffset(DateTime? value) =>
        value is { } dt ? new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)) : DateTimeOffset.UtcNow;

    private static string NormalisePrefix(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        return raw.EndsWith('/') ? raw : raw + "/";
    }

    private static AmazonS3Client BuildClient(S3AttachmentBlobOptions opts)
    {
        var config = new AmazonS3Config
        {
            ForcePathStyle = opts.ForcePathStyle,
            // AWS SDK v4 defaults to WHEN_SUPPORTED, which adds CRC32 to every PUT.
            // Older MinIO releases reject these — WHEN_REQUIRED keeps the SDK
            // compatible with non-AWS S3 implementations.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };
        if (!string.IsNullOrWhiteSpace(opts.ServiceUrl))
        {
            config.ServiceURL = opts.ServiceUrl;
            // SigV4 still needs a region for signing even when ServiceURL is a custom endpoint.
            config.AuthenticationRegion = opts.Region;
        }
        else
        {
            config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(opts.Region);
        }

        if (!string.IsNullOrEmpty(opts.AccessKey) && !string.IsNullOrEmpty(opts.SecretKey))
        {
            return new AmazonS3Client(opts.AccessKey, opts.SecretKey, config);
        }
        return new AmazonS3Client(config);
    }
}
