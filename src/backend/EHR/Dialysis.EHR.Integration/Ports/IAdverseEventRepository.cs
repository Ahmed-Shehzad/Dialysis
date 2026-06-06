using Dialysis.EHR.Integration.ReadModels;

namespace Dialysis.EHR.Integration.Ports;

/// <summary>
/// Read model of patient-safety adverse events, fed by the PDMS intradialytic adverse-event integration
/// event. Backs the cross-patient safety-surveillance dashboard.
/// </summary>
public interface IAdverseEventRepository
{
    /// <summary>Records an event idempotently on <see cref="AdverseEventRecord.SourceEventKey"/> (no-op if present).</summary>
    Task RecordAsync(AdverseEventRecord record, CancellationToken cancellationToken = default);

    /// <summary>Events at or after <paramref name="sinceUtc"/> (most-recent first), for surveillance windows.</summary>
    Task<IReadOnlyList<AdverseEventRecord>> ListSinceAsync(DateTime sinceUtc, int take, CancellationToken cancellationToken = default);

    /// <summary>Events for one patient (most-recent first).</summary>
    Task<IReadOnlyList<AdverseEventRecord>> ListForPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default);
}
