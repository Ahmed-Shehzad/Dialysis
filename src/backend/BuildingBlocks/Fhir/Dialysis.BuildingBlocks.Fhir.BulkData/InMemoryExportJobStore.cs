using System.Collections.Concurrent;

namespace Dialysis.BuildingBlocks.Fhir.BulkData;

public sealed class InMemoryExportJobStore : IExportJobStore
{
    private readonly ConcurrentDictionary<string, ExportJob> _jobs = new(StringComparer.Ordinal);

    public ValueTask<ExportJob> CreateAsync(ExportJob job, CancellationToken cancellationToken)
    {
        _jobs[job.Id] = job;
        return new ValueTask<ExportJob>(job);
    }

    public ValueTask<ExportJob?> GetAsync(string jobId, CancellationToken cancellationToken)
        => new(_jobs.GetValueOrDefault(jobId));

    public ValueTask UpdateAsync(ExportJob job, CancellationToken cancellationToken)
    {
        _jobs[job.Id] = job;
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<ExportJob>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var active = _jobs.Values
            .Where(j => j.Status is ExportJobStatus.Queued or ExportJobStatus.InProgress)
            .ToArray();
        return new ValueTask<IReadOnlyList<ExportJob>>(active);
    }
}
