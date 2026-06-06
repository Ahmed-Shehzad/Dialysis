using Dialysis.EHR.Integration.ReadModels;

namespace Dialysis.EHR.Integration.Ports;

/// <summary>
/// Billing-7c-style read model of hospital/encounter events, fed by HIS + HIE integration events. Drives
/// the facility-wide care-coordination follow-up worklist.
/// </summary>
public interface IHospitalEventRepository
{
    /// <summary>Records an event idempotently on <c>(Kind, SourceEventKey)</c> (no-op if already present).</summary>
    Task RecordAsync(HospitalEvent hospitalEvent, CancellationToken cancellationToken = default);

    /// <summary>Marks an event followed-up; returns false when the id is unknown.</summary>
    Task<bool> MarkFollowedUpAsync(Guid id, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Events still needing follow-up (most-recent first).</summary>
    Task<IReadOnlyList<HospitalEvent>> ListNeedsFollowUpAsync(int take, CancellationToken cancellationToken = default);

    /// <summary>Events for one patient (most-recent first), for the chart card.</summary>
    Task<IReadOnlyList<HospitalEvent>> ListForPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default);
}
