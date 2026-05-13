using Dialysis.EHR.PatientChart.Domain;

namespace Dialysis.EHR.PatientChart.Ports;

public interface IMedicationStatementRepository
{
    Task<MedicationStatement?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicationStatement>> ListByPatientAsync(Guid patientId, bool activeOnly, CancellationToken cancellationToken = default);
    void Add(MedicationStatement statement);
}
