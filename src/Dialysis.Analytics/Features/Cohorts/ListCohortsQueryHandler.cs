using Dialysis.Analytics.Data;
using Dialysis.Analytics.Services;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Cohorts;

public sealed class ListCohortsQueryHandler : IQueryHandler<ListCohortsQuery, IReadOnlyList<CohortSummaryDto>>
{
    private readonly ISavedCohortStore _store;
    private readonly IAnalyticsAuditRecorder _audit;

    public ListCohortsQueryHandler(ISavedCohortStore store, IAnalyticsAuditRecorder audit)
    {
        _store = store;
        _audit = audit;
    }

    public async Task<IReadOnlyList<CohortSummaryDto>> HandleAsync(ListCohortsQuery request, CancellationToken cancellationToken = default)
    {
        var cohorts = await _store.ListAsync(cancellationToken);
        await _audit.RecordAsync("Cohort", "list", "read", outcome: "0", cancellationToken: cancellationToken);
        return cohorts.Select(c => new CohortSummaryDto(c.Id, c.Name, c.Criteria, c.CreatedAt, c.UpdatedAt)).ToList();
    }
}
