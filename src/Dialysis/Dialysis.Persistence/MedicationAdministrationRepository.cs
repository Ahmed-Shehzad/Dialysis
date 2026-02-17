using Dialysis.Domain.Aggregates;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Persistence;

public sealed class MedicationAdministrationRepository : IMedicationAdministrationRepository
{
    private readonly DialysisDbContext _db;

    public MedicationAdministrationRepository(DialysisDbContext db) => _db = db;

    public async Task AddAsync(MedicationAdministration medication, CancellationToken cancellationToken = default)
    {
        await _db.MedicationAdministrations.AddAsync(medication, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MedicationAdministration?> GetAsync(TenantId tenantId, string id, CancellationToken cancellationToken = default)
        => await _db.MedicationAdministrations
            .FirstOrDefaultAsync(
                m => m.TenantId == tenantId && m.Id.ToString() == id,
                cancellationToken);

    public async Task<IReadOnlyList<MedicationAdministration>> ListByPatientAsync(
        TenantId tenantId,
        PatientId patientId,
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default)
        => await _db.MedicationAdministrations
            .Where(m => m.TenantId == tenantId && m.PatientId == patientId)
            .OrderByDescending(m => m.EffectiveAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MedicationAdministration>> ListBySessionAsync(
        TenantId tenantId,
        string sessionId,
        CancellationToken cancellationToken = default)
        => await _db.MedicationAdministrations
            .Where(m => m.TenantId == tenantId && m.SessionId == sessionId)
            .OrderBy(m => m.EffectiveAt)
            .ToListAsync(cancellationToken);
}
