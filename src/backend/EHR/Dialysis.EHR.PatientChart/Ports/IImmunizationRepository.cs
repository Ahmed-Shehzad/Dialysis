using Dialysis.EHR.PatientChart.Domain;

namespace Dialysis.EHR.PatientChart.Ports;

public interface IImmunizationRepository
{
    Task<IReadOnlyList<Immunization>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    void Add(Immunization immunization);
}
