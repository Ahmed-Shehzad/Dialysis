using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.PatientPortal.Domain;

public enum PortalAppointmentRequestStatus
{
    Pending = 1,
    Approved = 2,
    Declined = 3,
    Cancelled = 4,
}

public sealed class PortalAppointmentRequest : AggregateRoot<Guid>
{
    private PortalAppointmentRequest()
    {
    }

    public PortalAppointmentRequest(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public string ReasonText { get; private set; } = string.Empty;

    public DateTime EarliestPreferredUtc { get; private set; }

    public DateTime LatestPreferredUtc { get; private set; }

    public PortalAppointmentRequestStatus Status { get; private set; }

    public Guid? CreatedAppointmentId { get; private set; }

    public string? StaffNote { get; private set; }

    public static PortalAppointmentRequest Submit(
        Guid id,
        Guid patientId,
        string reasonText,
        DateTime earliestPreferredUtc,
        DateTime latestPreferredUtc)
    {
        if (patientId == Guid.Empty) throw new ArgumentException("Patient required.", nameof(patientId));
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonText);
        if (latestPreferredUtc <= earliestPreferredUtc)
            throw new ArgumentException("Latest preferred must be after earliest preferred.", nameof(latestPreferredUtc));
        if (earliestPreferredUtc < DateTime.UtcNow.AddDays(-1))
            throw new ArgumentException("Cannot request an appointment in the past.", nameof(earliestPreferredUtc));

        var request = new PortalAppointmentRequest(id)
        {
            PatientId = patientId,
            ReasonText = reasonText.Trim(),
            EarliestPreferredUtc = earliestPreferredUtc,
            LatestPreferredUtc = latestPreferredUtc,
            Status = PortalAppointmentRequestStatus.Pending,
        };

        request.RaiseIntegrationEvent(new PatientPortalAppointmentRequestedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            RequestId: id,
            PatientId: patientId,
            ReasonText: request.ReasonText,
            EarliestPreferredUtc: earliestPreferredUtc,
            LatestPreferredUtc: latestPreferredUtc));

        return request;
    }

    public void Approve(Guid createdAppointmentId, string? staffNote = null)
    {
        if (Status != PortalAppointmentRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot approve a request in status {Status}.");
        Status = PortalAppointmentRequestStatus.Approved;
        CreatedAppointmentId = createdAppointmentId;
        StaffNote = string.IsNullOrWhiteSpace(staffNote) ? null : staffNote.Trim();
    }

    public void Decline(string staffNote)
    {
        if (Status != PortalAppointmentRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot decline a request in status {Status}.");
        ArgumentException.ThrowIfNullOrWhiteSpace(staffNote);
        Status = PortalAppointmentRequestStatus.Declined;
        StaffNote = staffNote.Trim();
    }

    public void Cancel()
    {
        if (Status != PortalAppointmentRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot cancel a request in status {Status}.");
        Status = PortalAppointmentRequestStatus.Cancelled;
    }
}
