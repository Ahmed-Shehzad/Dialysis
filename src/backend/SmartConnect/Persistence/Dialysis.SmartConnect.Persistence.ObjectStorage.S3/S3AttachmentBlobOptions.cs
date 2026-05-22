namespace Dialysis.SmartConnect.Persistence.ObjectStorage.S3;

/// <summary>
/// Options for <see cref="S3AttachmentBlobStore"/>. Points at any S3-compatible endpoint —
/// AWS S3, MinIO, Wasabi, Backblaze B2, Cloudflare R2 — by supplying <see cref="ServiceUrl"/>
/// and <see cref="ForcePathStyle"/> appropriately. Production AWS deployments should leave
/// <see cref="AccessKey"/> / <see cref="SecretKey"/> unset and rely on the default credential
/// chain (IAM role, environment, profile).
/// </summary>
/// <remarks>
/// Mutable get/set rather than init-only because the <see cref="Microsoft.Extensions.Options"/>
/// pattern instantiates the options parameterlessly and applies configuration callbacks afterward.
/// The store constructor validates that <see cref="BucketName"/> is non-empty.
/// </remarks>
public sealed class S3AttachmentBlobOptions
{
    /// <summary>Bucket the attachments are stored in. Must already exist; the store does not auto-create.</summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>AWS region for the client, e.g. <c>us-east-1</c>. Ignored when <see cref="ServiceUrl"/> is set (MinIO etc.).</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Custom endpoint URL for S3-compatible services. Leave <c>null</c> for AWS S3 to use the
    /// region-derived endpoint; set to e.g. <c>http://minio:9000</c> for MinIO/MinIO-in-Aspire.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Force path-style addressing (<c>endpoint/bucket/key</c> rather than <c>bucket.endpoint/key</c>).
    /// Required for MinIO and most non-AWS S3-compatible servers; AWS S3 itself supports both.
    /// </summary>
    public bool ForcePathStyle { get; set; }

    /// <summary>
    /// Explicit access key. Leave <c>null</c> in AWS deployments — the SDK's default credential chain
    /// (IAM role, environment, shared credentials file) is preferred. Set for MinIO/local dev.
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>Counterpart to <see cref="AccessKey"/>. Both must be set together or both unset.</summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Prefix prepended to every object key. Useful for sharing a bucket across modules
    /// (e.g. <c>smartconnect/attachments/</c>). Trailing slash optional — the store normalises it.
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;
}
