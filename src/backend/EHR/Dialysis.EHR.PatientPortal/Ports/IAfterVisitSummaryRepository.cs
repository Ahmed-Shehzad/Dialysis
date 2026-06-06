using Dialysis.EHR.PatientPortal.Domain;

namespace Dialysis.EHR.PatientPortal.Ports;

public interface IAfterVisitSummaryRepository
{
    Task<AfterVisitSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AfterVisitSummary>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    void Add(AfterVisitSummary summary);
}
