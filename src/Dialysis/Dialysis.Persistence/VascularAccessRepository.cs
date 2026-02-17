using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Persistence;

public sealed class VascularAccessRepository : IVascularAccessRepository
{
    private readonly DialysisDbContext _db;

    public VascularAccessRepository(DialysisDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(VascularAccess access, CancellationToken cancellationToken = default)
    {
        _db.VascularAccess.Add(access);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<VascularAccess?> GetByIdAsync(TenantId tenantId, Ulid id, CancellationToken cancellationToken = default)
    {
        return await _db.VascularAccess
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<VascularAccess>> GetByPatientAsync(TenantId tenantId, PatientId patientId, VascularAccessStatus? status = null, CancellationToken cancellationToken = default)
    {
        IQueryable<VascularAccess> query = _db.VascularAccess
            .Where(a => a.TenantId == tenantId && a.PatientId == patientId);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        return await query
            .OrderByDescending(a => a.PlacementDate ?? a.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}
