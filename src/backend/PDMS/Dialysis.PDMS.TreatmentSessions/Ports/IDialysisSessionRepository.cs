using Dialysis.PDMS.TreatmentSessions.Domain;

namespace Dialysis.PDMS.TreatmentSessions.Ports;

public interface IDialysisSessionRepository
{
    Task<DialysisSession?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DialysisSession>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DialysisSession>> ListRecentAsync(DateTime sinceUtc, int take, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DialysisSession>> ListActiveAsync(CancellationToken cancellationToken = default);

    void Add(DialysisSession session);
}
