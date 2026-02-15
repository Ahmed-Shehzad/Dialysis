using Dialysis.Alerting.Data;
using Dialysis.Alerting.Services;
using Intercessor.Abstractions;

namespace Dialysis.Alerting.Features.ProcessAlerts;

public sealed class CreateAlertHandler : ICommandHandler<CreateAlertCommand, CreateAlertResult>
{
    private readonly ITenantAlertDbContextFactory _dbFactory;
    private readonly IAlertCacheService _cache;

    public CreateAlertHandler(ITenantAlertDbContextFactory dbFactory, IAlertCacheService cache)
    {
        _dbFactory = dbFactory;
        _cache = cache;
    }

    public async Task<CreateAlertResult> HandleAsync(CreateAlertCommand request, CancellationToken cancellationToken = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var alertId = Ulid.NewUlid().ToString();
        var alert = new Alert
        {
            Id = alertId,
            PatientId = request.PatientId,
            EncounterId = request.EncounterId,
            Code = request.Code,
            Severity = request.Severity,
            Message = request.Message,
            Status = Data.AlertStatus.Active,
            RaisedAt = DateTimeOffset.UtcNow
        };

        db.Alerts.Add(alert);
        await db.SaveChangesAsync(cancellationToken);

        await _cache.InvalidateListAsync(cancellationToken);

        return new CreateAlertResult { AlertId = alertId };
    }
}
