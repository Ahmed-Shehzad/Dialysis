using Intercessor.Abstractions;

namespace Dialysis.Alerting.Features.ProcessAlerts;

public sealed record ListAlertsQuery : IQuery<ListAlertsResult>
{
    /// <summary>Filter by status: Active, Acknowledged, or null for all.</summary>
    public AlertStatusFilter? Status { get; init; }
}

public enum AlertStatusFilter
{
    Active,
    Acknowledged
}

public sealed record ListAlertsResult
{
    public required IReadOnlyList<AlertSummaryDto> Alerts { get; init; }
}
