using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

public sealed class EfAuditEventStore(SmartConnectDbContext db) : IAuditEventStore
{
    public async Task RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        db.AuditEvents.Add(new AuditEventEntity
        {
            Id = auditEvent.Id,
            Timestamp = auditEvent.Timestamp,
            Category = (int)auditEvent.Category,
            Level = (int)auditEvent.Level,
            FlowId = auditEvent.FlowId,
            UserId = auditEvent.UserId,
            Summary = auditEvent.Summary,
            AttributesJson = auditEvent.AttributesJson,
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AuditEvent>> QueryAsync(
        AuditEventCategory? category = null,
        AuditEventLevel? level = null,
        Guid? flowId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = db.AuditEvents.AsQueryable();
        if (category.HasValue) query = query.Where(e => e.Category == (int)category.Value);
        if (level.HasValue) query = query.Where(e => e.Level == (int)level.Value);
        if (flowId.HasValue) query = query.Where(e => e.FlowId == flowId.Value);
        if (from.HasValue) query = query.Where(e => e.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(e => e.Timestamp <= to.Value);

        var entities = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.Select(e => new AuditEvent
        {
            Id = e.Id,
            Timestamp = e.Timestamp,
            Category = (AuditEventCategory)e.Category,
            Level = (AuditEventLevel)e.Level,
            FlowId = e.FlowId,
            UserId = e.UserId,
            Summary = e.Summary,
            AttributesJson = e.AttributesJson,
        }).ToList();
    }

    public async Task<AuditEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await db.AuditEvents.FindAsync([id], cancellationToken).ConfigureAwait(false);
        if (e is null) return null;
        return new AuditEvent
        {
            Id = e.Id,
            Timestamp = e.Timestamp,
            Category = (AuditEventCategory)e.Category,
            Level = (AuditEventLevel)e.Level,
            FlowId = e.FlowId,
            UserId = e.UserId,
            Summary = e.Summary,
            AttributesJson = e.AttributesJson,
        };
    }
}
