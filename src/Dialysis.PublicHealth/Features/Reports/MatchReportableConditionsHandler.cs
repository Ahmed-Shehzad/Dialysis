using Dialysis.PublicHealth.Services;
using Intercessor.Abstractions;

namespace Dialysis.PublicHealth.Features.Reports;

public sealed class MatchReportableConditionsHandler : IQueryHandler<MatchReportableConditionsQuery, IReadOnlyList<ReportableConditionMatch>>
{
    private readonly ReportableConditionMatcher _matcher;

    public MatchReportableConditionsHandler(ReportableConditionMatcher matcher)
    {
        _matcher = matcher;
    }

    public Task<IReadOnlyList<ReportableConditionMatch>> HandleAsync(MatchReportableConditionsQuery request, CancellationToken cancellationToken = default)
        => _matcher.MatchAsync(request.Resource, request.Jurisdiction, cancellationToken);
}
