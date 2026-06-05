using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Fhir.BulkData.ObjectStorage;

/// <summary>
/// <see cref="IBulkDataStorage"/> over Azure Blob / Azurite. Uses the SDK's native writable/readable
/// streams. Blob name layout is <c>{prefix}{jobId}/{resourceType}.ndjson</c>.
/// </summary>
public sealed class AzureBlobBulkDataStorage : IBulkDataStorage
{
    private readonly BlobContainerClient _container;
    private readonly string _prefix;

    /// <summary>Builds the container client from options (connection string for dev, managed identity in prod).</summary>
    public AzureBlobBulkDataStorage(IOptions<AzureBlobBulkDataStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var o = options.Value;
        _prefix = o.KeyPrefix ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(o.ConnectionString))
        {
            _container = new BlobContainerClient(o.ConnectionString, o.ContainerName);
        }
        else if (o.ServiceUri is not null)
        {
            var service = new BlobServiceClient(o.ServiceUri, new DefaultAzureCredential());
            _container = service.GetBlobContainerClient(o.ContainerName);
        }
        else
        {
            throw new InvalidOperationException(
                "AzureBlobBulkDataStorageOptions requires either ConnectionString or ServiceUri.");
        }
    }

    /// <inheritdoc />
    public async ValueTask<Stream> OpenWriteAsync(string jobId, string resourceType, CancellationToken cancellationToken)
    {
        await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var blob = _container.GetBlobClient(KeyOf(jobId, resourceType + ".ndjson"));
        return await blob.OpenWriteAsync(overwrite: true, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Stream> OpenReadAsync(string jobId, string fileName, CancellationToken cancellationToken)
    {
        var blob = _container.GetBlobClient(KeyOf(jobId, fileName));
        return await blob.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string BuildOutputUrl(string jobId, string resourceType) =>
        _container.GetBlobClient(KeyOf(jobId, resourceType + ".ndjson")).Uri.ToString();

    private string KeyOf(string jobId, string fileName) => $"{_prefix}{Sanitize(jobId)}/{Sanitize(fileName)}";

    private static string Sanitize(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment)
            || segment != Path.GetFileName(segment)
            || segment.Contains("..", StringComparison.Ordinal)
            || Path.IsPathRooted(segment))
        {
            throw new ArgumentException($"Invalid bulk-data path segment: '{segment}'.", nameof(segment));
        }
        return segment;
    }
}
