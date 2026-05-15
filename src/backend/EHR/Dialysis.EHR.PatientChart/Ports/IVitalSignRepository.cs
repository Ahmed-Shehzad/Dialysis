using Dialysis.EHR.PatientChart.Domain;

namespace Dialysis.EHR.PatientChart.Ports;

public interface IVitalSignRepository
{
    Task<IReadOnlyList<VitalSignReading>> ListByPatientAsync(Guid patientId, DateTime sinceUtc, CancellationToken cancellationToken = default);
    void Add(VitalSignReading reading);

    /// <summary>
    /// Streams every vital-sign reading for bulk export, honouring <paramref name="since"/>
    /// against <c>ObservedAtUtc</c>.
    /// </summary>
    IAsyncEnumerable<VitalSignReading> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default);
}
