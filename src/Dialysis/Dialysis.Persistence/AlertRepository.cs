using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.Persistence;

public sealed class AlertRepository : IAlertRepository
{
    private readonly DialysisDbContext _db;

    public AlertRepository(DialysisDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        _db.Alerts.Add(alert);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Alert?> GetByIdAsync(TenantId tenantId, Ulid alertId, CancellationToken cancellationToken = default)
    {
        return await _db.Alerts
            .FirstOrDefaultAsync(
                a => a.TenantId == tenantId && a.Id == alertId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Alert>> GetByPatientAsync(TenantId tenantId, PatientId patientId, bool? activeOnly = null, int? limit = null, int offset = 0, CancellationToken cancellationToken = default)
    {
        IQueryable<Alert> query = _db.Alerts
            .Where(a => a.TenantId == tenantId && a.PatientId == patientId);

        if (activeOnly == true)
            query = query.Where(a => a.Status == AlertStatus.Active);

        query = query.OrderByDescending(a => a.CreatedAtUtc).Skip(offset);
        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}
