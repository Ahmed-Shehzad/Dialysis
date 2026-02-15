using Dialysis.Analytics.Data;
using Dialysis.Analytics.Services;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Cohorts;

public sealed class ResolveSavedCohortQueryHandler : IQueryHandler<ResolveSavedCohortQuery, ResolveSavedCohortResult?>
{
    private readonly ISavedCohortStore _store;
    private readonly Intercessor.Abstractions.ISender _sender;
    private readonly IAnalyticsAuditRecorder _audit;

    public ResolveSavedCohortQueryHandler(ISavedCohortStore store, Intercessor.Abstractions.ISender sender, IAnalyticsAuditRecorder audit)
    {
        _store = store;
        _sender = sender;
        _audit = audit;
    }

    public async Task<ResolveSavedCohortResult?> HandleAsync(ResolveSavedCohortQuery request, CancellationToken cancellationToken = default)
    {
        var cohort = await _store.GetByIdAsync(request.CohortId, cancellationToken);
        if (cohort == null) return null;

        var result = await _sender.SendAsync(new ResolveCohortQuery(cohort.Criteria), cancellationToken);
        await _audit.RecordAsync("Cohort", request.CohortId, "resolve", outcome: "0", cancellationToken: cancellationToken);
        return new ResolveSavedCohortResult(
            request.CohortId,
            cohort.Name,
            result.PatientIds,
            result.EncounterIds,
            result.TotalPatients,
            result.TotalEncounters);
    }
}
