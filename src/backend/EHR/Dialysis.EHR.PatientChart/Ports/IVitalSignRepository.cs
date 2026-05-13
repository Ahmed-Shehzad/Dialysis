using Dialysis.EHR.PatientChart.Domain;

namespace Dialysis.EHR.PatientChart.Ports;

public interface IVitalSignRepository
{
    Task<IReadOnlyList<VitalSignReading>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default);
    void Add(VitalSignReading reading);
}
