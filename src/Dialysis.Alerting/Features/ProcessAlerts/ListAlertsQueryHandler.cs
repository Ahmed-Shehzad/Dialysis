using Dialysis.Alerting.Data;
using Dialysis.Alerting.Services;
using Intercessor.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.Alerting.Features.ProcessAlerts;

public sealed class ListAlertsQueryHandler : IQueryHandler<ListAlertsQuery, ListAlertsResult>
{
    private readonly ITenantAlertDbContextFactory _dbFactory;
    private readonly IAlertCacheService _cache;

    public ListAlertsQueryHandler(ITenantAlertDbContextFactory dbFactory, IAlertCacheService cache)
    {
        _dbFactory = dbFactory;
        _cache = cache;
    }

    public async Task<ListAlertsResult> HandleAsync(ListAlertsQuery request, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetListAsync(request.Status, cancellationToken);
        if (cached is not null)
        {
            return new ListAlertsResult { Alerts = cached };
        }

        await using var db = _dbFactory.CreateDbContext();
        var query = db.Alerts.AsNoTracking();

        if (request.Status.HasValue)
        {
            var status = request.Status.Value switch
            {
                AlertStatusFilter.Active => Data.AlertStatus.Active,
                AlertStatusFilter.Acknowledged => Data.AlertStatus.Acknowledged,
                _ => (Data.AlertStatus?)null
            };
            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }
        }

        var alerts = await query
            .OrderByDescending(a => a.RaisedAt)
            .Select(a => new AlertSummaryDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                EncounterId = a.EncounterId,
                Code = a.Code,
                Severity = a.Severity,
                Message = a.Message,
                Status = a.Status.ToString(),
                RaisedAt = a.RaisedAt,
                AcknowledgedAt = a.AcknowledgedAt
            })
            .ToListAsync(cancellationToken);

        await _cache.SetListAsync(request.Status, alerts, cancellationToken);

        return new ListAlertsResult { Alerts = alerts };
    }
}
