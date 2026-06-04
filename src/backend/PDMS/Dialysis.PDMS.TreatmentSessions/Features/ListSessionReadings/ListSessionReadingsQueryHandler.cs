using Dialysis.CQRS.Queries;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Dialysis.PDMS.TreatmentSessions.Realtime;

namespace Dialysis.PDMS.TreatmentSessions.Features.ListSessionReadings;

public sealed class ListSessionReadingsQueryHandler : IQueryHandler<ListSessionReadingsQuery, IReadOnlyList<VitalsReadingSnapshot>>
{
    private readonly IDialysisSessionRepository _sessions;
    public ListSessionReadingsQueryHandler(IDialysisSessionRepository sessions) => _sessions = sessions;
    public async Task<IReadOnlyList<VitalsReadingSnapshot>> HandleAsync(
        ListSessionReadingsQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(request.SessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Session '{request.SessionId}' not found.");

        var limit = Math.Clamp(request.Limit, 1, 5000);

        return [.. session.Readings
            .OrderByDescending(r => r.ObservedAtUtc)
            .Take(limit)
            .Select(r => new VitalsReadingSnapshot(
                r.Id,
                r.SessionId,
                r.ObservedAtUtc,
                r.SystolicBloodPressure,
                r.DiastolicBloodPressure,
                r.HeartRateBpm,
                r.ArterialPressureMmHg,
                r.VenousPressureMmHg,
                r.UltrafiltrationRateMlPerHour,
                r.ConductivityMsPerCm,
                r.Notes))];
    }
}
