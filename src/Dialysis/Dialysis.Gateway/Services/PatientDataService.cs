using Dialysis.Persistence;
using Dialysis.Persistence.Queries;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Gateway.Services;

/// <summary>
/// Aggregates patient data using DbContext directly with AsNoTracking for read-only queries.
/// </summary>
public sealed class PatientDataService : IPatientDataService
{
    private readonly DialysisDbContext _db;

    public PatientDataService(DialysisDbContext db) => _db = db;

    public async Task<PatientDataAggregate?> GetAsync(TenantId tenantId, PatientId patientId, CancellationToken cancellationToken = default)
    {
        var tenantStr = tenantId.Value;
        var patientStr = patientId.Value;

        var patient = await CompiledQueries.GetPatientById(_db, tenantStr, patientStr);
        if (patient is null)
            return null;

        var observations = await _db.Observations
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId && o.PatientId == patientId)
            .OrderByDescending(o => o.Effective.Value)
            .ToListAsync(cancellationToken);

        var sessions = await _db.Sessions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.PatientId == patientId)
            .OrderByDescending(s => s.StartedAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        var conditions = await _db.Conditions
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.PatientId == patientId)
            .OrderByDescending(c => c.RecordedDate)
            .ToListAsync(cancellationToken);

        var episodes = await _db.EpisodeOfCare
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.PatientId == patientId)
            .OrderByDescending(e => e.PeriodStart)
            .ToListAsync(cancellationToken);

        var meds = await _db.MedicationAdministrations
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.PatientId == patientId)
            .OrderByDescending(m => m.EffectiveAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        return new PatientDataAggregate(patient, observations, sessions, conditions, episodes, meds);
    }
}
