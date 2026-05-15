using Dialysis.HIS.PatientFlow.Domain;

namespace Dialysis.HIS.PatientFlow.Ports;

public interface IAdmissionRepository
{
    void Add(Admission admission);

    Task<Admission?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams every <see cref="Admission"/>, ordered for stable bulk-export output.
    /// When <paramref name="since"/> is provided, only admissions whose latest
    /// activity (discharge if present, otherwise admit) is at-or-after the cutoff are returned.
    /// </summary>
    IAsyncEnumerable<Admission> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams every distinct PatientId referenced by an Admission, optionally filtered to
    /// admissions whose latest discharge/admit activity is at-or-after <paramref name="since"/>.
    /// Used by the HIS Patient bulk-export feeder to emit stub Patient resources for the
    /// patients HIS knows about (EHR remains the patient-identity system of record).
    /// </summary>
    IAsyncEnumerable<Guid> StreamDistinctPatientIdsAsync(DateTimeOffset? since, CancellationToken cancellationToken = default);
}
