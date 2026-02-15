using Dialysis.AuditConsent.Data;
using Dialysis.Tenancy;
using Intercessor.Abstractions;

namespace Dialysis.AuditConsent.Features.Audit;

public sealed class RecordAuditHandler : ICommandHandler<RecordAuditCommand>
{
    private readonly ITenantAuditDbContextFactory _dbFactory;
    private readonly ITenantContext _tenantContext;

    public RecordAuditHandler(ITenantAuditDbContextFactory dbFactory, ITenantContext tenantContext)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
    }

    public async Task HandleAsync(RecordAuditCommand request, CancellationToken cancellationToken = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var tenantId = _tenantContext.TenantId ?? "default";
        var entity = new AuditEventEntity
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            TenantId = tenantId,
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            Action = request.Action,
            AgentId = request.AgentId,
            Outcome = request.Outcome,
            RecordedAt = DateTimeOffset.UtcNow
        };
        db.AuditEvents.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}
