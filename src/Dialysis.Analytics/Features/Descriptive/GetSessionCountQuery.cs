using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Descriptive;

public sealed record GetSessionCountQuery(DateOnly? From, DateOnly? To) : IQuery<GetSessionCountResult>;

public sealed record GetSessionCountResult(
    string Metric,
    DateOnly From,
    DateOnly To,
    int Value);
