using Dialysis.AuditConsent.Data;
using Dialysis.Tenancy;
using Intercessor.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.AuditConsent.Features.Audit;

public sealed class GetAuditQueryHandler : IQueryHandler<GetAuditQuery, GetAuditResult>
{
    private readonly ITenantAuditDbContextFactory _dbFactory;

    public GetAuditQueryHandler(ITenantAuditDbContextFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<GetAuditResult> HandleAsync(GetAuditQuery request, CancellationToken cancellationToken = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var query = db.AuditEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.ResourceType))
            query = query.Where(e => e.ResourceType == request.ResourceType);
        if (!string.IsNullOrWhiteSpace(request.ResourceId))
            query = query.Where(e => e.ResourceId == request.ResourceId);

        var entries = await query
            .OrderByDescending(e => e.RecordedAt)
            .Select(e => new AuditEntryDto
            {
                Id = e.Id,
                ResourceType = e.ResourceType,
                ResourceId = e.ResourceId,
                Action = e.Action,
                AgentId = e.AgentId,
                Outcome = e.Outcome,
                RecordedAt = e.RecordedAt
            })
            .ToListAsync(cancellationToken);

        return new GetAuditResult { Entries = entries };
    }
}
