using Dialysis.ApiClients;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Descriptive;

public sealed class GetAlertStatsQueryHandler : IQueryHandler<GetAlertStatsQuery, GetAlertStatsResult>
{
    private readonly IAlertingApi _alertingApi;

    public GetAlertStatsQueryHandler(IAlertingApi alertingApi)
    {
        _alertingApi = alertingApi;
    }

    public async Task<GetAlertStatsResult> HandleAsync(GetAlertStatsQuery request, CancellationToken cancellationToken = default)
    {
        var allAlerts = await _alertingApi.GetAlertsAsync(cancellationToken);
        var alerts = (request.From.HasValue || request.To.HasValue)
            ? allAlerts.Where(a =>
            {
                var d = DateOnly.FromDateTime(a.RaisedAt.DateTime);
                if (request.From.HasValue && d < request.From.Value) return false;
                if (request.To.HasValue && d > request.To.Value) return false;
                return true;
            }).ToList()
            : allAlerts.ToList();

        var active = alerts.Count(a => string.Equals(a.Status, "Active", StringComparison.OrdinalIgnoreCase));
        var ack = alerts.Count(a => string.Equals(a.Status, "Acknowledged", StringComparison.OrdinalIgnoreCase));

        var ackTimes = alerts
            .Where(a => a.AcknowledgedAt.HasValue)
            .Select(a => (a.AcknowledgedAt!.Value - a.RaisedAt).TotalSeconds)
            .OrderBy(t => t)
            .ToList();

        double? medianSeconds = null;
        if (ackTimes.Count > 0)
        {
            var mid = ackTimes.Count / 2;
            medianSeconds = ackTimes.Count % 2 == 1
                ? ackTimes[mid]
                : (ackTimes[mid - 1] + ackTimes[mid]) / 2;
        }

        return new GetAlertStatsResult(
            "alert_stats", request.From, request.To,
            alerts.Count, active, ack, medianSeconds);
    }
}
