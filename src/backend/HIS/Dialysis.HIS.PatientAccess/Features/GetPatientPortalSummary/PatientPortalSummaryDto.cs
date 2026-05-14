namespace Dialysis.HIS.PatientAccess.Features.GetPatientPortalSummary;

public sealed record PatientPortalSummaryDto(
    Guid PatientId,
    int UpcomingAppointmentCount,
    int OpenMedicationOrderCount,
    int OpenAdmissionCount);
