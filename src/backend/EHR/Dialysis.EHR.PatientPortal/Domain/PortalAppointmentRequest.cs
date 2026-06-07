using Dialysis.DomainDrivenDesign.Exceptions;
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
        if (patientId == Guid.Empty) throw new DomainException("Patient required.");
        if (string.IsNullOrWhiteSpace(reasonText)) throw new DomainException("Reason required.");
        if (latestPreferredUtc <= earliestPreferredUtc)
            throw new DomainException("Latest preferred must be after earliest preferred.");
        if (earliestPreferredUtc < DateTime.UtcNow.AddDays(-1))
            throw new DomainException("Cannot request an appointment in the past.");

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
            throw new DomainException($"Cannot approve a request in status {Status}.");
        Status = PortalAppointmentRequestStatus.Approved;
        CreatedAppointmentId = createdAppointmentId;
        StaffNote = string.IsNullOrWhiteSpace(staffNote) ? null : staffNote.Trim();
        RaiseResolved(approved: true);
    }

    public void Decline(string staffNote)
    {
        if (Status != PortalAppointmentRequestStatus.Pending)
            throw new DomainException($"Cannot decline a request in status {Status}.");
        if (string.IsNullOrWhiteSpace(staffNote)) throw new DomainException("Staff note required.");
        Status = PortalAppointmentRequestStatus.Declined;
        StaffNote = staffNote.Trim();
        RaiseResolved(approved: false);
    }

    private void RaiseResolved(bool approved) =>
        RaiseIntegrationEvent(new PatientPortalAppointmentResolvedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            RequestId: Id,
            PatientId: PatientId,
            Approved: approved,
            CreatedAppointmentId: CreatedAppointmentId,
            StaffNote: StaffNote));

    public void Cancel()
    {
        if (Status != PortalAppointmentRequestStatus.Pending)
            throw new DomainException($"Cannot cancel a request in status {Status}.");
        Status = PortalAppointmentRequestStatus.Cancelled;
    }
}
