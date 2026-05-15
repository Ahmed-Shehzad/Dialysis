using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.PDMS.Persistence.Stores;

public sealed class DialysisSessionRepository(PdmsDbContext db) : IDialysisSessionRepository
{
    public Task<DialysisSession?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Sessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DialysisSession>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default) =>
        await db.Sessions
            .Where(s => s.PatientId == patientId && s.ScheduledStartUtc >= sinceUtc)
            .OrderByDescending(s => s.ScheduledStartUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<DialysisSession>> ListRecentAsync(
        DateTime sinceUtc,
        int take,
        CancellationToken cancellationToken = default) =>
        await db.Sessions
            .Where(s => s.ScheduledStartUtc >= sinceUtc)
            .OrderByDescending(s => s.ScheduledStartUtc)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<DialysisSession>> ListActiveAsync(
        CancellationToken cancellationToken = default) =>
        await db.Sessions
            .Where(s => s.Status == DialysisSessionStatus.InProgress
                     || s.Status == DialysisSessionStatus.Scheduled
                     || s.Status == DialysisSessionStatus.Paused)
            .OrderBy(s => s.ScheduledStartUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(DialysisSession session) => db.Sessions.Add(session);
}
