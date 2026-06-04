namespace Dialysis.BuildingBlocks.Fhir.BulkData;

/// <summary>
/// Writes NDJSON output files under <c>{ContentRoot}/bulk-data/{jobId}/{resourceType}.ndjson</c>.
/// </summary>
public sealed class LocalFileBulkDataStorage : IBulkDataStorage
{
    private readonly string _rootPath;
    /// <summary>
    /// Writes NDJSON output files under <c>{ContentRoot}/bulk-data/{jobId}/{resourceType}.ndjson</c>.
    /// </summary>
    public LocalFileBulkDataStorage(string rootPath) => _rootPath = rootPath;
    public ValueTask<Stream> OpenWriteAsync(string jobId, string resourceType, CancellationToken cancellationToken)
    {
        var dir = Path.Combine(_rootPath, SafeSegment(jobId));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{SafeSegment(resourceType)}.ndjson");
        return ValueTask.FromResult<Stream>(File.Create(path));
    }

    public ValueTask<Stream> OpenReadAsync(string jobId, string fileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_rootPath, SafeSegment(jobId), SafeSegment(fileName));
        return ValueTask.FromResult<Stream>(File.OpenRead(path));
    }

    public string BuildOutputUrl(string jobId, string resourceType) => $"/fhir/bulk-data/jobs/{jobId}/files/{resourceType}.ndjson";

    /// <summary>
    /// Ensures a URL/job-derived path segment is a single, relative file/dir name — rejecting
    /// directory separators, <c>..</c>, and rooted paths so a crafted jobId/fileName can't make
    /// <see cref="Path.Combine(string[])"/> escape <c>rootPath</c> (path traversal).
    /// </summary>
    private static string SafeSegment(string segment)
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
