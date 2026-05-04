namespace Dialysis.HIS.PatientAccess.Ports;

/// <summary>Portal consent flags persisted per patient (absence of a row = legacy implicit allow-all).</summary>
public sealed record PatientPortalConsentState(bool SummaryVisible, bool AppointmentRequestsAllowed);

public interface IPatientPortalConsentReadModel
{
    Task<PatientPortalConsentState?> GetAsync(Guid patientId, CancellationToken cancellationToken = default);
}
