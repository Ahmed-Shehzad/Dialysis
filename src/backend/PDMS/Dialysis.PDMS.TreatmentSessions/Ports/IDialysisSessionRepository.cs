using Dialysis.PDMS.TreatmentSessions.Domain;

namespace Dialysis.PDMS.TreatmentSessions.Ports;

public interface IDialysisSessionRepository
{
    Task<DialysisSession?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DialysisSession>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DialysisSession>> ListRecentAsync(DateTime sinceUtc, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DialysisSession>> ListActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams every <see cref="DialysisSession"/> ordered by <c>ScheduledStartUtc</c> for stable
    /// bulk-export NDJSON output. When <paramref name="since"/> is provided, only sessions whose
    /// scheduled start (or actual start if present) is at-or-after the cutoff are returned.
    /// </summary>
    IAsyncEnumerable<DialysisSession> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default);

    void Add(DialysisSession session);
}
