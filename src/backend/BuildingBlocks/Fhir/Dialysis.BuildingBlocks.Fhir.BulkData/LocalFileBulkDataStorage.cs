namespace Dialysis.BuildingBlocks.Fhir.BulkData;

/// <summary>
/// Writes NDJSON output files under <c>{ContentRoot}/bulk-data/{jobId}/{resourceType}.ndjson</c>.
/// </summary>
public sealed class LocalFileBulkDataStorage(string rootPath) : IBulkDataStorage
{
    public ValueTask<Stream> OpenWriteAsync(string jobId, string resourceType, CancellationToken cancellationToken)
    {
        var dir = Path.Combine(rootPath, jobId);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{resourceType}.ndjson");
        return ValueTask.FromResult<Stream>(File.Create(path));
    }

    public ValueTask<Stream> OpenReadAsync(string jobId, string fileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(rootPath, jobId, fileName);
        return ValueTask.FromResult<Stream>(File.OpenRead(path));
    }

    public string BuildOutputUrl(string jobId, string resourceType) => $"/fhir/bulk-data/jobs/{jobId}/files/{resourceType}.ndjson";
}
