using Dialysis.EHR.PatientPortal.Domain;

namespace Dialysis.EHR.PatientPortal.Ports;

public interface IPortalAppointmentRequestRepository
{
    Task<PortalAppointmentRequest?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a still-<see cref="PortalAppointmentRequestStatus.Pending"/> request for the same patient,
    /// reason and preferred window, if one exists — used to make submission idempotent so retries,
    /// double-taps, and the dev data-simulator don't stack identical duplicates on the staff worklist.
    /// </summary>
    Task<PortalAppointmentRequest?> FindOpenDuplicateAsync(
        Guid patientId,
        string reasonText,
        DateTime earliestPreferredUtc,
        DateTime latestPreferredUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PortalAppointmentRequest>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortalAppointmentRequest>> ListByStatusAsync(PortalAppointmentRequestStatus status, int take, CancellationToken cancellationToken = default);
    void Add(PortalAppointmentRequest request);
}
