using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Dialysis.SmartConnect.Attachments;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Persistence.ObjectStorage.S3;

/// <summary>
/// Mints SigV4 presigned GET URLs for attachments stored via <see cref="S3AttachmentBlobStore"/>.
/// Suitable for AWS S3 and any S3-compatible service that respects presigned URLs (MinIO, Wasabi,
/// R2, B2). The URL embeds the signature; the client fetches the bytes directly from S3 without
/// re-streaming through the API host.
/// </summary>
public sealed class S3PresignedAttachmentDownloadUrlFactory : IAttachmentDownloadUrlFactory
{
    private readonly AmazonS3Client _client;
    private readonly string _bucket;
    private readonly string _prefix;

    public S3PresignedAttachmentDownloadUrlFactory(IOptions<S3AttachmentBlobOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.BucketName))
        {
            throw new ArgumentException("BucketName must be configured.", nameof(options));
        }
        _bucket = opts.BucketName;
        _prefix = string.IsNullOrWhiteSpace(opts.KeyPrefix)
            ? string.Empty
            : (opts.KeyPrefix.EndsWith('/') ? opts.KeyPrefix : opts.KeyPrefix + "/");

        // BuildClient lives in S3AttachmentBlobStore as private; rebuild the client here from the
        // same options so the presigner uses the same endpoint / region / credentials.
        var config = new AmazonS3Config
        {
            ForcePathStyle = opts.ForcePathStyle,
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };
        if (!string.IsNullOrWhiteSpace(opts.ServiceUrl))
        {
            config.ServiceURL = opts.ServiceUrl;
            config.AuthenticationRegion = opts.Region;
        }
        else
        {
            config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(opts.Region);
        }
        _client = !string.IsNullOrEmpty(opts.AccessKey) && !string.IsNullOrEmpty(opts.SecretKey)
            ? new AmazonS3Client(opts.AccessKey, opts.SecretKey, config)
            : new AmazonS3Client(config);
    }

    public bool SupportsSignedUrls => true;

    public async Task<Uri?> CreateAsync(Guid attachmentId, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = _prefix + attachmentId.ToString("D"),
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(ttl),
            Protocol = Protocol.HTTPS,
        };
        var url = await _client.GetPreSignedURLAsync(request).ConfigureAwait(false);
        return string.IsNullOrEmpty(url) ? null : new Uri(url);
    }
}
