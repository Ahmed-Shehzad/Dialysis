namespace Dialysis.BuildingBlocks.Fhir.BulkData.ObjectStorage;

/// <summary>S3 / MinIO sink configuration for Bulk Data NDJSON output.</summary>
public sealed class S3BulkDataStorageOptions
{
    /// <summary>Bucket the NDJSON files are written to.</summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>AWS region (ignored when <see cref="ServiceUrl"/> targets MinIO).</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>MinIO / custom S3 endpoint; null for real AWS.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>MinIO requires path-style addressing.</summary>
    public bool ForcePathStyle { get; set; }

    /// <summary>Access key; leave null to use the ambient credential chain (IAM role / env).</summary>
    public string? AccessKey { get; set; }

    /// <summary>Secret key; paired with <see cref="AccessKey"/>.</summary>
    public string? SecretKey { get; set; }

    /// <summary>Key prefix, e.g. <c>fhir-export/</c>. Objects are <c>{prefix}{jobId}/{resourceType}.ndjson</c>.</summary>
    public string KeyPrefix { get; set; } = "fhir-export/";
}

/// <summary>Azure Blob / Azurite sink configuration for Bulk Data NDJSON output.</summary>
public sealed class AzureBlobBulkDataStorageOptions
{
    /// <summary>Container the NDJSON blobs are written to.</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Connection string (Azurite / dev). Mutually exclusive with <see cref="ServiceUri"/>.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Account blob endpoint (production; uses <c>DefaultAzureCredential</c>).</summary>
    public Uri? ServiceUri { get; set; }

    /// <summary>Blob name prefix; blobs are <c>{prefix}{jobId}/{resourceType}.ndjson</c>.</summary>
    public string KeyPrefix { get; set; } = "fhir-export/";
}
