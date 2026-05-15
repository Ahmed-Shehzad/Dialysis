using Dialysis.EHR.PatientChart.Domain;

namespace Dialysis.EHR.PatientChart.Ports;

public interface IAllergyRepository
{
    Task<Allergy?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Allergy>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    void Add(Allergy allergy);

    /// <summary>
    /// Streams every allergy for bulk export. The aggregate currently lacks a last-modified
    /// timestamp, so <paramref name="since"/> is reserved and ignored — same trade-off the EHR
    /// Patient feeder used before <c>UpdatedAtUtc</c> was added.
    /// </summary>
    IAsyncEnumerable<Allergy> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default);
}
