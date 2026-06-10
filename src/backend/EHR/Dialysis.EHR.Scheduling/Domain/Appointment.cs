using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.Scheduling.Domain;

public sealed class Appointment : AggregateRoot<Guid>
{
    private Appointment()
    {
    }

    public Appointment(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public Guid ProviderId { get; private set; }

    public DateTime StartUtc { get; private set; }

    public DateTime EndUtc { get; private set; }

    public string EncounterClassCode { get; private set; } = string.Empty;

    public string? VisitReason { get; private set; }

    public AppointmentStatus Status { get; private set; }

    public DateTime? CheckedInAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public string? CancellationReasonCode { get; private set; }

    public static Appointment Book(
        Guid id,
        Guid patientId,
        Guid providerId,
        DateTime startUtc,
        DateTime endUtc,
        string encounterClassCode,
        string? visitReason)
    {
        if (patientId == Guid.Empty)
            throw new DomainException("Patient required.");
        if (providerId == Guid.Empty)
            throw new DomainException("Provider required.");
        if (endUtc <= startUtc)
            throw new DomainException("End must follow start.");
        if (startUtc < DateTime.UtcNow.AddMinutes(-5))
            throw new DomainException("Cannot book an appointment in the past.");
        if (string.IsNullOrWhiteSpace(encounterClassCode))
            throw new DomainException("Encounter class code required.");

        var appointment = new Appointment(id)
        {
            PatientId = patientId,
            ProviderId = providerId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            EncounterClassCode = encounterClassCode.Trim(),
            VisitReason = string.IsNullOrWhiteSpace(visitReason) ? null : visitReason.Trim(),
            Status = AppointmentStatus.Scheduled,
        };

        appointment.RaiseIntegrationEvent(new AppointmentBookedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            AppointmentId: id,
            PatientId: patientId,
            ProviderId: providerId,
            StartUtc: startUtc,
            EndUtc: endUtc,
            EncounterClassCode: appointment.EncounterClassCode,
            VisitReason: appointment.VisitReason));

        return appointment;
    }

    public void Reschedule(DateTime newStartUtc, DateTime newEndUtc)
    {
        if (Status != AppointmentStatus.Scheduled)
            throw new DomainException($"Cannot reschedule appointment in status {Status}.");
        if (newEndUtc <= newStartUtc)
            throw new DomainException("End must follow start.");

        StartUtc = newStartUtc;
        EndUtc = newEndUtc;

        RaiseIntegrationEvent(new AppointmentRescheduledIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            AppointmentId: Id,
            PatientId: PatientId,
            NewStartUtc: newStartUtc,
            NewEndUtc: newEndUtc));
    }

    public void Cancel(string reasonCode)
    {
        if (Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
            throw new DomainException($"Cannot cancel appointment in status {Status}.");
        if (string.IsNullOrWhiteSpace(reasonCode))
            throw new DomainException("Cancellation reason required.");

        Status = AppointmentStatus.Cancelled;
        CancellationReasonCode = reasonCode.Trim();

        RaiseIntegrationEvent(new AppointmentCancelledIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            AppointmentId: Id,
            PatientId: PatientId,
            ReasonCode: CancellationReasonCode));
    }

    public void CheckIn(DateTime checkedInAtUtc)
    {
        if (Status != AppointmentStatus.Scheduled)
            throw new DomainException($"Cannot check in appointment in status {Status}.");

        Status = AppointmentStatus.CheckedIn;
        CheckedInAtUtc = checkedInAtUtc;

        RaiseIntegrationEvent(new AppointmentCheckedInIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            AppointmentId: Id,
            PatientId: PatientId,
            CheckedInAtUtc: checkedInAtUtc));
    }

    public void Start()
    {
        if (Status != AppointmentStatus.CheckedIn)
            throw new DomainException("Patient must be checked in first.");
        Status = AppointmentStatus.InProgress;
    }

    public void Complete(DateTime completedAtUtc)
    {
        if (Status != AppointmentStatus.InProgress)
            throw new DomainException("Appointment must be in progress to complete.");
        Status = AppointmentStatus.Completed;
        CompletedAtUtc = completedAtUtc;
    }

    public void MarkNoShow()
    {
        if (Status != AppointmentStatus.Scheduled)
            throw new DomainException($"Cannot mark no-show in status {Status}.");
        Status = AppointmentStatus.NoShow;
    }
}
