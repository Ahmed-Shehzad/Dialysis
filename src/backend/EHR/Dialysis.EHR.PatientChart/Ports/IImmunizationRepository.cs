using Dialysis.EHR.PatientChart.Domain;

namespace Dialysis.EHR.PatientChart.Ports;

public interface IImmunizationRepository
{
    Task<IReadOnlyList<Immunization>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    void Add(Immunization immunization);

    /// <summary>
    /// Streams every immunization for bulk export, honouring <paramref name="since"/> against
    /// the <c>AdministeredOn</c> date (compared at the start of the day).
    /// </summary>
    IAsyncEnumerable<Immunization> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default);
}
