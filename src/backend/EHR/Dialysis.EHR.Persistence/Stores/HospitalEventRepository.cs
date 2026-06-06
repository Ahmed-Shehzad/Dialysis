using Dialysis.EHR.Integration.Ports;
using Dialysis.EHR.Integration.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Stores;

public sealed class HospitalEventRepository : IHospitalEventRepository
{
    private readonly EhrDbContext _db;
    public HospitalEventRepository(EhrDbContext db) => _db = db;

    public async Task RecordAsync(HospitalEvent hospitalEvent, CancellationToken cancellationToken = default)
    {
        var exists = await _db.HospitalEvents
            .AnyAsync(e => e.Kind == hospitalEvent.Kind && e.SourceEventKey == hospitalEvent.SourceEventKey, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
            _db.HospitalEvents.Add(hospitalEvent);
    }

    public async Task<bool> MarkFollowedUpAsync(Guid id, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var affected = await _db.HospitalEvents
            .Where(e => e.Id == id && !e.FollowedUp)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.FollowedUp, true)
                .SetProperty(e => e.FollowedUpAtUtc, nowUtc), cancellationToken)
            .ConfigureAwait(false);
        if (affected > 0)
            return true;
        // Already followed-up or unknown id — distinguish "exists" from "missing".
        return await _db.HospitalEvents.AnyAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<HospitalEvent>> ListNeedsFollowUpAsync(int take, CancellationToken cancellationToken = default)
    {
        var bounded = Math.Clamp(take, 1, 500);
        return await _db.HospitalEvents
            .AsNoTracking()
            .Where(e => !e.FollowedUp)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(bounded)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<HospitalEvent>> ListForPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default)
    {
        var bounded = Math.Clamp(take, 1, 500);
        return await _db.HospitalEvents
            .AsNoTracking()
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(bounded)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
