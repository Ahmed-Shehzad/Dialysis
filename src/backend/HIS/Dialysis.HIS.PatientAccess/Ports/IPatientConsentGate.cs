namespace Dialysis.HIS.PatientAccess.Ports;

/// <summary>Consent / visibility rules for patient-facing reads and writes.</summary>
public interface IPatientConsentGate
{
    Task EnsureCanViewSummaryAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>Patient-initiated appointment requests; default implementation mirrors summary visibility.</summary>
    Task EnsureCanRequestAppointmentAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        EnsureCanViewSummaryAsync(patientId, cancellationToken);
}
