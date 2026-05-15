namespace Dialysis.BuildingBlocks.Fhir.BulkData;

/// <summary>
/// Default <see cref="IExportJobOrchestrator"/>: persists jobs via <see cref="IExportJobStore"/>
/// and hands the job id off to <see cref="ExportJobQueue"/> for in-process background execution.
/// </summary>
public sealed class DefaultExportJobOrchestrator(
    IExportJobStore store,
    ExportJobQueue queue,
    TimeProvider time) : IExportJobOrchestrator
{
    public async ValueTask<ExportJob> EnqueueAsync(
        ExportScope scope,
        IReadOnlyList<string> resourceTypes,
        DateTimeOffset? since,
        string? groupId,
        string? requestorId,
        string? deIdentificationProfile,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resourceTypes);

        var job = new ExportJob(
            Id: Guid.NewGuid().ToString("N"),
            Scope: scope,
            GroupId: groupId,
            ResourceTypes: resourceTypes,
            Since: since,
            DeIdentificationProfile: deIdentificationProfile,
            RequestorId: requestorId,
            Status: ExportJobStatus.Queued,
            CreatedAt: time.GetUtcNow(),
            CompletedAt: null,
            Outputs: Array.Empty<ExportJobOutput>(),
            Error: null);

        var persisted = await store.CreateAsync(job, cancellationToken).ConfigureAwait(false);
        await queue.EnqueueAsync(persisted.Id, cancellationToken).ConfigureAwait(false);
        return persisted;
    }

    public async ValueTask CancelAsync(string jobId, CancellationToken cancellationToken)
    {
        var job = await store.GetAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (job is null)
        {
            return;
        }
        if (job.Status is ExportJobStatus.Completed or ExportJobStatus.Failed or ExportJobStatus.Cancelled)
        {
            return;
        }

        var cancelled = job with { Status = ExportJobStatus.Cancelled, CompletedAt = time.GetUtcNow() };
        await store.UpdateAsync(cancelled, cancellationToken).ConfigureAwait(false);
    }
}
