using Dialysis.Analytics.Data;
using Dialysis.Analytics.Services;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Cohorts;

public sealed class GetCohortQueryHandler : IQueryHandler<GetCohortQuery, SavedCohort?>
{
    private readonly ISavedCohortStore _store;
    private readonly IAnalyticsAuditRecorder _audit;

    public GetCohortQueryHandler(ISavedCohortStore store, IAnalyticsAuditRecorder audit)
    {
        _store = store;
        _audit = audit;
    }

    public async Task<SavedCohort?> HandleAsync(GetCohortQuery request, CancellationToken cancellationToken = default)
    {
        var cohort = await _store.GetByIdAsync(request.Id, cancellationToken);
        if (cohort != null)
            await _audit.RecordAsync("Cohort", request.Id, "read", outcome: "0", cancellationToken: cancellationToken);
        return cohort;
    }
}
