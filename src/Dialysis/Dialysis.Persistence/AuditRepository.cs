using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Persistence;

public sealed class AuditRepository : IAuditRepository
{
    private readonly DialysisDbContext _db;

    public AuditRepository(DialysisDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        _db.AuditEvents.Add(auditEvent);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> QueryAsync(
        TenantId tenantId,
        string? patientId = null,
        string? resourceType = null,
        string? action = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int limit = 100,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AuditEvent> query = _db.AuditEvents
            .Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(patientId))
            query = query.Where(e => e.PatientId == patientId);
        if (!string.IsNullOrWhiteSpace(resourceType))
            query = query.Where(e => e.ResourceType == resourceType);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(e => e.Action == action);
        if (fromUtc.HasValue)
            query = query.Where(e => e.CreatedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue)
            query = query.Where(e => e.CreatedAtUtc <= toUtc.Value);

        return await query
            .OrderByDescending(e => e.CreatedAtUtc)
            .Skip(offset)
            .Take(Math.Min(limit, 500))
            .ToListAsync(cancellationToken);
    }
}
