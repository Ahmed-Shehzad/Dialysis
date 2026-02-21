using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Treatment.Infrastructure.Persistence;

public sealed class PreAssessmentRepository : IPreAssessmentRepository
{
    private readonly TreatmentDbContext _db;
    private readonly ITenantContext _tenant;

    public PreAssessmentRepository(TreatmentDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<PreAssessment?> GetBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await _db.PreAssessments
            .FirstOrDefaultAsync(
                p => p.TenantId == _tenant.TenantId && p.SessionId == sessionId,
                cancellationToken);
    }

    public async Task AddAsync(PreAssessment entity, CancellationToken cancellationToken = default)
    {
        _ = await _db.PreAssessments.AddAsync(entity, cancellationToken);
    }

    public void Update(PreAssessment entity)
    {
        _ = _db.PreAssessments.Update(entity);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _ = await _db.SaveChangesAsync(cancellationToken);
    }
}
