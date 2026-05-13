using Dialysis.EHR.PatientChart.Domain;

namespace Dialysis.EHR.PatientChart.Ports;

public interface IAllergyRepository
{
    Task<Allergy?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allergy>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    void Add(Allergy allergy);
}
