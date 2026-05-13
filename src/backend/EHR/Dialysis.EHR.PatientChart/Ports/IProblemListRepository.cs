using Dialysis.EHR.PatientChart.Domain;

namespace Dialysis.EHR.PatientChart.Ports;

public interface IProblemListRepository
{
    Task<ProblemListItem?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProblemListItem>> ListByPatientAsync(Guid patientId, bool activeOnly, CancellationToken cancellationToken = default);
    void Add(ProblemListItem item);
}
