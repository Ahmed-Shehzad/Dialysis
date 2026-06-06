using Dialysis.EHR.PatientChart.Domain;

namespace Dialysis.EHR.PatientChart.Ports;

public interface ICarePlanRepository
{
    Task<CarePlan?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the patient's single active care plan (goals included), or null.</summary>
    Task<CarePlan?> GetActiveByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);

    void Add(CarePlan carePlan);

    /// <summary>
    /// Streams every care plan (goals included) for FHIR bulk export. Honours <paramref name="since"/>
    /// against the plan's creation timestamp.
    /// </summary>
    IAsyncEnumerable<CarePlan> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default);
}
