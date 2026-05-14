namespace Dialysis.BuildingBlocks.Fhir.BulkData;

public interface IExportJobStore
{
    ValueTask<ExportJob> CreateAsync(ExportJob job, CancellationToken cancellationToken);

    ValueTask<ExportJob?> GetAsync(string jobId, CancellationToken cancellationToken);

    ValueTask UpdateAsync(ExportJob job, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<ExportJob>> ListActiveAsync(CancellationToken cancellationToken);
}

public interface IExportJobOrchestrator
{
    /// <summary>
    /// Enqueues an export job for asynchronous execution. Returns the persisted job with a unique id
    /// and <see cref="ExportJobStatus.Queued"/> status.
    /// </summary>
    ValueTask<ExportJob> EnqueueAsync(
        ExportScope scope,
        IReadOnlyList<string> resourceTypes,
        DateTimeOffset? since,
        string? groupId,
        string? requestorId,
        string? deIdentificationProfile,
        CancellationToken cancellationToken);

    ValueTask CancelAsync(string jobId, CancellationToken cancellationToken);
}

public interface IBulkDataStorage
{
    /// <summary>Open a writable stream for the NDJSON output file of <paramref name="resourceType"/> in <paramref name="jobId"/>.</summary>
    ValueTask<Stream> OpenWriteAsync(string jobId, string resourceType, CancellationToken cancellationToken);

    ValueTask<Stream> OpenReadAsync(string jobId, string fileName, CancellationToken cancellationToken);

    string BuildOutputUrl(string jobId, string resourceType);
}
