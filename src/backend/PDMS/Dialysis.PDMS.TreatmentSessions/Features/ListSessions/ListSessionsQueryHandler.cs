using Dialysis.CQRS.Queries;
using Dialysis.PDMS.TreatmentSessions.Ports;

namespace Dialysis.PDMS.TreatmentSessions.Features.ListSessions;

public sealed class ListSessionsQueryHandler : IQueryHandler<ListSessionsQuery, IReadOnlyList<DialysisSessionListItem>>
{
    private readonly IDialysisSessionRepository _sessions;
    private readonly TimeProvider _time;
    public ListSessionsQueryHandler(IDialysisSessionRepository sessions, TimeProvider time)
    {
        _sessions = sessions;
        _time = time;
    }
    public async Task<IReadOnlyList<DialysisSessionListItem>> HandleAsync(
        ListSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 500);
        var data = request.ActiveOnly
            ? await _sessions.ListActiveAsync(cancellationToken).ConfigureAwait(false)
            : await _sessions.ListRecentAsync(_time.GetUtcNow().UtcDateTime.AddDays(-7), take, cancellationToken).ConfigureAwait(false);

        return [.. data
            .Take(take)
            .Select(s => new DialysisSessionListItem(
                s.Id,
                s.PatientId,
                s.Status.ToString(),
                s.ScheduledStartUtc,
                s.ActualStartUtc,
                s.ActualEndUtc,
                s.MachineId,
                s.PausedAtUtc,
                (int)s.AccumulatedPausedDuration.TotalSeconds))];
    }
}
