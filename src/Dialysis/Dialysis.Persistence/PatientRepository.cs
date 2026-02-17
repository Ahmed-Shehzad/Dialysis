using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Persistence;

public sealed class PatientRepository : IPatientRepository
{
    private readonly DialysisDbContext _db;

    public PatientRepository(DialysisDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Patient?> GetByIdAsync(TenantId tenantId, PatientId logicalId, CancellationToken cancellationToken = default)
    {
        return await _db.Patients
            .FirstOrDefaultAsync(
                p => p.TenantId == tenantId && p.LogicalId == logicalId,
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(TenantId tenantId, PatientId logicalId, CancellationToken cancellationToken = default)
    {
        return await _db.Patients
            .AnyAsync(
                p => p.TenantId == tenantId && p.LogicalId == logicalId,
                cancellationToken);
    }

    public async Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _db.Patients.Update(patient);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _db.Patients.Remove(patient);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Patient>> ListAsync(TenantId tenantId, string? family = null, string? given = null, int? limit = null, int offset = 0, CancellationToken cancellationToken = default)
    {
        IQueryable<Patient> query = _db.Patients
            .Where(p => p.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(family))
            query = query.Where(p => p.FamilyName != null && p.FamilyName.ToLower().Contains(family.ToLower()));
        if (!string.IsNullOrWhiteSpace(given))
            query = query.Where(p => p.GivenNames != null && p.GivenNames.ToLower().Contains(given.ToLower()));

        query = query
            .OrderBy(p => p.FamilyName)
            .ThenBy(p => p.LogicalId)
            .Skip(offset);
        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync(cancellationToken);
    }
}
