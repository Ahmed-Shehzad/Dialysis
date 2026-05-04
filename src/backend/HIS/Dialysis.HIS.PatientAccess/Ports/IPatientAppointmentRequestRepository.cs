namespace Dialysis.HIS.PatientAccess.Ports;

public sealed class PatientAppointmentRequest
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public string Notes { get; set; } = string.Empty;

    public DateTime RequestedAtUtc { get; set; }
}

public interface IPatientAppointmentRequestRepository
{
    void Add(PatientAppointmentRequest request);
}
