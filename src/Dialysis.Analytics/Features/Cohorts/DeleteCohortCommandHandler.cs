using Dialysis.Analytics.Data;
using Dialysis.Analytics.Services;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Cohorts;

public sealed class DeleteCohortCommandHandler : ICommandHandler<DeleteCohortCommand, bool>
{
    private readonly ISavedCohortStore _store;
    private readonly IAnalyticsAuditRecorder _audit;

    public DeleteCohortCommandHandler(ISavedCohortStore store, IAnalyticsAuditRecorder audit)
    {
        _store = store;
        _audit = audit;
    }

    public async Task<bool> HandleAsync(DeleteCohortCommand request, CancellationToken cancellationToken = default)
    {
        var deleted = await _store.DeleteAsync(request.Id, cancellationToken);
        if (deleted)
            await _audit.RecordAsync("Cohort", request.Id, "delete", outcome: "0", cancellationToken: cancellationToken);
        return deleted;
    }
}
