using Dialysis.Alerting.Data;
using Dialysis.Alerting.Services;
using Intercessor.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.Alerting.Features.ProcessAlerts;

public sealed class AcknowledgeAlertHandler : ICommandHandler<AcknowledgeAlertCommand>
{
    private readonly ITenantAlertDbContextFactory _dbFactory;
    private readonly IAlertCacheService _cache;

    public AcknowledgeAlertHandler(ITenantAlertDbContextFactory dbFactory, IAlertCacheService cache)
    {
        _dbFactory = dbFactory;
        _cache = cache;
    }

    public async Task HandleAsync(AcknowledgeAlertCommand request, CancellationToken cancellationToken = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var alert = await db.Alerts.FirstOrDefaultAsync(a => a.Id == request.AlertId, cancellationToken);
        if (alert is null)
        {
            return;
        }

        alert.Status = AlertStatus.Acknowledged;
        alert.AcknowledgedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await _cache.InvalidateListAsync(cancellationToken);
    }
}
