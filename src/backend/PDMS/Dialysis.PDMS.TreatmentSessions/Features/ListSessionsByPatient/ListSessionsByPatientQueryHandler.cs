using Dialysis.CQRS.Queries;
using Dialysis.PDMS.TreatmentSessions.Features.ListSessions;
using Dialysis.PDMS.TreatmentSessions.Ports;

namespace Dialysis.PDMS.TreatmentSessions.Features.ListSessionsByPatient;

public sealed class ListSessionsByPatientQueryHandler(
    IDialysisSessionRepository sessions,
    TimeProvider time)
    : IQueryHandler<ListSessionsByPatientQuery, IReadOnlyList<DialysisSessionListItem>>
{
    public async Task<IReadOnlyList<DialysisSessionListItem>> HandleAsync(
        ListSessionsByPatientQuery request,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 200);
        var lookbackDays = Math.Clamp(request.LookbackDays, 1, 365);
        var sinceUtc = time.GetUtcNow().UtcDateTime.AddDays(-lookbackDays);

        var rows = await sessions
            .ListByPatientAsync(request.PatientId, sinceUtc, cancellationToken)
            .ConfigureAwait(false);

        return [.. rows
            .OrderByDescending(s => s.ActualStartUtc ?? s.ScheduledStartUtc)
            .Take(take)
            .Select(s => new DialysisSessionListItem(
                s.Id,
                s.PatientId,
                s.Status.ToString(),
                s.ScheduledStartUtc,
                s.ActualStartUtc,
                s.ActualEndUtc,
                s.MachineId))];
    }
}
