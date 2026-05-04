namespace Dialysis.HIS.PatientAccess.Ports;

public sealed record PatientPortalSummaryDto(Guid PatientId, string MedicalRecordNumber, string VisitState, string? Message);

public interface IPatientPortalSummaryReadModel
{
    Task<PatientPortalSummaryDto?> GetAsync(Guid patientId, CancellationToken cancellationToken = default);
}
