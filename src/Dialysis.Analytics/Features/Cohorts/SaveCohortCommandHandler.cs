using Dialysis.Analytics.Data;
using Dialysis.Analytics.Services;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Cohorts;

public sealed class SaveCohortCommandHandler : ICommandHandler<SaveCohortCommand, SavedCohort>
{
    private readonly ISavedCohortStore _store;
    private readonly IAnalyticsAuditRecorder _audit;

    public SaveCohortCommandHandler(ISavedCohortStore store, IAnalyticsAuditRecorder audit)
    {
        _store = store;
        _audit = audit;
    }

    public async Task<SavedCohort> HandleAsync(SaveCohortCommand request, CancellationToken cancellationToken = default)
    {
        var cohort = new SavedCohort
        {
            Id = request.Id ?? "",
            Name = request.Name ?? "Unnamed cohort",
            Criteria = request.Criteria ?? new CohortCriteria()
        };
        var saved = await _store.SaveAsync(cohort, cancellationToken);
        await _audit.RecordAsync("Cohort", saved.Id, string.IsNullOrEmpty(request.Id) ? "create" : "update", outcome: "0", cancellationToken: cancellationToken);
        return saved;
    }
}
