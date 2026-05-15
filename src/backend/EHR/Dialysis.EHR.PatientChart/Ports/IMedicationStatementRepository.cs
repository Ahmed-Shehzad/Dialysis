using Dialysis.EHR.PatientChart.Domain;

namespace Dialysis.EHR.PatientChart.Ports;

public interface IMedicationStatementRepository
{
    Task<MedicationStatement?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicationStatement>> ListByPatientAsync(Guid patientId, bool activeOnly, CancellationToken cancellationToken = default);
    void Add(MedicationStatement statement);

    /// <summary>
    /// Streams every medication statement for bulk export. Honours <paramref name="since"/>
    /// against <c>StoppedOn ?? StartedOn</c> (compared at the start of the day).
    /// </summary>
    IAsyncEnumerable<MedicationStatement> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default);
}
