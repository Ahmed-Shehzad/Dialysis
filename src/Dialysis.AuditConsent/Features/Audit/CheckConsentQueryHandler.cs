using Dialysis.AuditConsent.Data;
using Dialysis.Tenancy;
using Intercessor.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.AuditConsent.Features.Audit;

public sealed class CheckConsentQueryHandler : IQueryHandler<CheckConsentQuery, CheckConsentResult>
{
    private readonly ITenantAuditDbContextFactory _dbFactory;
    private readonly ITenantContext _tenantContext;

    public CheckConsentQueryHandler(ITenantAuditDbContextFactory dbFactory, ITenantContext tenantContext)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
    }

    public async Task<CheckConsentResult> HandleAsync(CheckConsentQuery request, CancellationToken cancellationToken = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var tenantId = _tenantContext.TenantId ?? "default";

        var hasConsent = await db.AuditEvents
            .AsNoTracking()
            .AnyAsync(e =>
                e.TenantId == tenantId &&
                e.ResourceType == request.ResourceType &&
                e.ResourceId == request.ResourceId &&
                e.Action == request.Action &&
                (e.Outcome == null || e.Outcome == "0"),
                cancellationToken);

        return new CheckConsentResult(hasConsent);
    }
}
