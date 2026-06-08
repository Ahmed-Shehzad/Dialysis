using Dialysis.HIS.PatientAccess.Features.GetPatientPortalSummary;

namespace Dialysis.HIS.PatientAccess.Ports;

public interface IPatientPortalReadModel
{
    Task<PatientPortalSummaryDto> GetSummaryAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists distinct patient ids that have portal-relevant HIS data (a booked appointment, an open
    /// medication order, or an open admission) — i.e. the patients a portal summary would render
    /// non-empty for. Lets the portal SPA / smoke discover which patient to open without a patient
    /// claim. Capped at <paramref name="take"/>.
    /// </summary>
    Task<IReadOnlyList<Guid>> ListPatientIdsAsync(int take, CancellationToken cancellationToken = default);
}
