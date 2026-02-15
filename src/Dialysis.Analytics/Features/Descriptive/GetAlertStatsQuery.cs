using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Descriptive;

public sealed record GetAlertStatsQuery(DateOnly? From, DateOnly? To) : IQuery<GetAlertStatsResult>;

public sealed record GetAlertStatsResult(
    string Metric,
    DateOnly? From,
    DateOnly? To,
    int TotalCount,
    int ActiveCount,
    int AcknowledgedCount,
    double? MedianTimeToAckSeconds);
