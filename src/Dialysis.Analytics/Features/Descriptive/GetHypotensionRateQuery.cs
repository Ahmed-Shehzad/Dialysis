using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Descriptive;

public sealed record GetHypotensionRateQuery(DateOnly? From, DateOnly? To) : IQuery<GetHypotensionRateResult>;

public sealed record GetHypotensionRateResult(
    string Metric,
    DateOnly From,
    DateOnly To,
    int TotalEncounters,
    int EncountersWithHypotension,
    double RatePercent);
