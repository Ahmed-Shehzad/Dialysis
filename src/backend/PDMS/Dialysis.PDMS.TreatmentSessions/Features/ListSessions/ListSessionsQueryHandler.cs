using Dialysis.CQRS.Queries;
using Dialysis.PDMS.TreatmentSessions.Ports;

namespace Dialysis.PDMS.TreatmentSessions.Features.ListSessions;

public sealed class ListSessionsQueryHandler(IDialysisSessionRepository sessions, TimeProvider time)
    : IQueryHandler<ListSessionsQuery, IReadOnlyList<DialysisSessionListItem>>
{
    public async Task<IReadOnlyList<DialysisSessionListItem>> HandleAsync(
        ListSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 500);
        var data = request.ActiveOnly
            ? await sessions.ListActiveAsync(cancellationToken).ConfigureAwait(false)
            : await sessions.ListRecentAsync(time.GetUtcNow().UtcDateTime.AddDays(-7), take, cancellationToken).ConfigureAwait(false);

        return data
            .Take(take)
            .Select(s => new DialysisSessionListItem(
                s.Id,
                s.PatientId,
                s.Status.ToString(),
                s.ScheduledStartUtc,
                s.ActualStartUtc,
                s.ActualEndUtc,
                s.MachineId))
            .ToList();
    }
}
