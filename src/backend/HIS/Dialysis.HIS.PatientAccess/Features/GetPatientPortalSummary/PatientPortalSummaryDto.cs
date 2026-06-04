namespace Dialysis.HIS.PatientAccess.Features.GetPatientPortalSummary;

public sealed record PatientPortalSummaryDto
{
    public PatientPortalSummaryDto(Guid PatientId,
        int UpcomingAppointmentCount,
        int OpenMedicationOrderCount,
        int OpenAdmissionCount)
    {
        this.PatientId = PatientId;
        this.UpcomingAppointmentCount = UpcomingAppointmentCount;
        this.OpenMedicationOrderCount = OpenMedicationOrderCount;
        this.OpenAdmissionCount = OpenAdmissionCount;
    }
    public Guid PatientId { get; init; }
    public int UpcomingAppointmentCount { get; init; }
    public int OpenMedicationOrderCount { get; init; }
    public int OpenAdmissionCount { get; init; }
    public void Deconstruct(out Guid PatientId, out int UpcomingAppointmentCount, out int OpenMedicationOrderCount, out int OpenAdmissionCount)
    {
        PatientId = this.PatientId;
        UpcomingAppointmentCount = this.UpcomingAppointmentCount;
        OpenMedicationOrderCount = this.OpenMedicationOrderCount;
        OpenAdmissionCount = this.OpenAdmissionCount;
    }
}
