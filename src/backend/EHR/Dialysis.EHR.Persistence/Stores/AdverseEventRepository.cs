using Dialysis.EHR.Integration.Ports;
using Dialysis.EHR.Integration.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Stores;

public sealed class AdverseEventRepository : IAdverseEventRepository
{
    private readonly EhrDbContext _db;
    public AdverseEventRepository(EhrDbContext db) => _db = db;

    public async Task RecordAsync(AdverseEventRecord record, CancellationToken cancellationToken = default)
    {
        var exists = await _db.AdverseEvents
            .AnyAsync(e => e.SourceEventKey == record.SourceEventKey, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
            _db.AdverseEvents.Add(record);
    }

    public async Task<IReadOnlyList<AdverseEventRecord>> ListSinceAsync(DateTime sinceUtc, int take, CancellationToken cancellationToken = default)
    {
        var bounded = Math.Clamp(take, 1, 5000);
        return await _db.AdverseEvents
            .AsNoTracking()
            .Where(e => e.OccurredAtUtc >= sinceUtc)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(bounded)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AdverseEventRecord>> ListForPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default)
    {
        var bounded = Math.Clamp(take, 1, 500);
        return await _db.AdverseEvents
            .AsNoTracking()
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(bounded)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
