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
        ArgumentNullException.ThrowIfNull(record);

        // Fast-path dedup: an equal SourceEventKey row already exists, so this re-delivery is a no-op.
        var exists = await _db.AdverseEvents
            .AnyAsync(e => e.SourceEventKey == record.SourceEventKey, cancellationToken)
            .ConfigureAwait(false);
        if (exists)
            return;

        // Own the save so a concurrent insert that slipped past the read above can't surface the
        // unique SourceEventKey violation to the caller. The projector's trailing SaveChangesAsync
        // then sees no pending AdverseEvent change.
        _db.AdverseEvents.Add(record);
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            // A concurrent ingest committed the same SourceEventKey between our read and this save.
            // Detach the doomed insert and treat the conflict as idempotent — unless the re-read can't
            // find a winner, in which case the failure was something else and must surface.
            _db.Entry(record).State = EntityState.Detached;
            var winner = await _db.AdverseEvents
                .AsNoTracking()
                .AnyAsync(e => e.SourceEventKey == record.SourceEventKey, cancellationToken)
                .ConfigureAwait(false);
            if (!winner)
                throw;
        }
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
