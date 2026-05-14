using Dialysis.HIS.PatientAccess.Features.GetPatientPortalSummary;

namespace Dialysis.HIS.PatientAccess.Ports;

public interface IPatientPortalReadModel
{
    Task<PatientPortalSummaryDto> GetSummaryAsync(Guid patientId, CancellationToken cancellationToken = default);
}
