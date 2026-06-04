using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.BuildingBlocks.Fhir.BulkData.EntityFrameworkCore;

/// <summary>
/// Persists <see cref="ExportJob"/> records on the host module's <typeparamref name="TDbContext"/>
/// under the <c>fhir_export</c> schema. Modules add <see cref="ExportJobRecordConfiguration"/> to
/// their <c>OnModelCreating</c> override to enable this store.
/// </summary>
public sealed class EfExportJobStore<TDbContext> : IExportJobStore
    where TDbContext : DbContext
{
    private readonly TDbContext _db;
    /// <summary>
    /// Persists <see cref="ExportJob"/> records on the host module's <typeparamref name="TDbContext"/>
    /// under the <c>fhir_export</c> schema. Modules add <see cref="ExportJobRecordConfiguration"/> to
    /// their <c>OnModelCreating</c> override to enable this store.
    /// </summary>
    public EfExportJobStore(TDbContext db) => _db = db;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public async ValueTask<ExportJob> CreateAsync(ExportJob job, CancellationToken cancellationToken)
    {
        var record = ToRecord(job);
        _db.Set<ExportJobRecord>().Add(record);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return job;
    }

    public async ValueTask<ExportJob?> GetAsync(string jobId, CancellationToken cancellationToken)
    {
        var record = await _db.Set<ExportJobRecord>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == jobId, cancellationToken).ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async ValueTask UpdateAsync(ExportJob job, CancellationToken cancellationToken)
    {
        var record = await _db.Set<ExportJobRecord>()
            .FirstOrDefaultAsync(r => r.Id == job.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Export job '{job.Id}' not found.");
        record.Scope = job.Scope;
        record.GroupId = job.GroupId;
        record.ResourceTypesCsv = string.Join(',', job.ResourceTypes);
        record.Since = job.Since;
        record.DeIdentificationProfile = job.DeIdentificationProfile;
        record.RequestorId = job.RequestorId;
        record.Status = job.Status;
        record.CompletedAt = job.CompletedAt;
        record.Error = job.Error;
        record.OutputsJson = JsonSerializer.Serialize(job.Outputs, _json);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<ExportJob>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var records = await _db.Set<ExportJobRecord>().AsNoTracking()
            .Where(r => r.Status == ExportJobStatus.Queued || r.Status == ExportJobStatus.InProgress)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return records.ConvertAll(ToDomain);
    }

    private static ExportJobRecord ToRecord(ExportJob job) => new()
    {
        Id = job.Id,
        Scope = job.Scope,
        GroupId = job.GroupId,
        ResourceTypesCsv = string.Join(',', job.ResourceTypes),
        Since = job.Since,
        DeIdentificationProfile = job.DeIdentificationProfile,
        RequestorId = job.RequestorId,
        Status = job.Status,
        CreatedAt = job.CreatedAt,
        CompletedAt = job.CompletedAt,
        Error = job.Error,
        OutputsJson = JsonSerializer.Serialize(job.Outputs, _json),
    };

    private static ExportJob ToDomain(ExportJobRecord record)
    {
        var outputs = string.IsNullOrEmpty(record.OutputsJson)
            ? []
            : JsonSerializer.Deserialize<List<ExportJobOutput>>(record.OutputsJson, _json) ?? [];
        var types = string.IsNullOrEmpty(record.ResourceTypesCsv)
            ? []
            : (IReadOnlyList<string>)record.ResourceTypesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return new ExportJob(
            Id: record.Id,
            Scope: record.Scope,
            GroupId: record.GroupId,
            ResourceTypes: types,
            Since: record.Since,
            DeIdentificationProfile: record.DeIdentificationProfile,
            RequestorId: record.RequestorId,
            Status: record.Status,
            CreatedAt: record.CreatedAt,
            CompletedAt: record.CompletedAt,
            Outputs: outputs,
            Error: record.Error);
    }
}
