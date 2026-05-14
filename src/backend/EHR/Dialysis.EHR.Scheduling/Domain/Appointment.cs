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
        if (patientId == Guid.Empty) throw new ArgumentException("Patient required.", nameof(patientId));
        if (providerId == Guid.Empty) throw new ArgumentException("Provider required.", nameof(providerId));
        if (endUtc <= startUtc) throw new ArgumentException("End must follow start.", nameof(endUtc));
        if (startUtc < DateTime.UtcNow.AddMinutes(-5)) throw new ArgumentException("Cannot book an appointment in the past.", nameof(startUtc));
        ArgumentException.ThrowIfNullOrWhiteSpace(encounterClassCode);

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
            throw new InvalidOperationException($"Cannot reschedule appointment in status {Status}.");
        if (newEndUtc <= newStartUtc) throw new ArgumentException("End must follow start.", nameof(newEndUtc));

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
            throw new InvalidOperationException($"Cannot cancel appointment in status {Status}.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);

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
            throw new InvalidOperationException($"Cannot check in appointment in status {Status}.");

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
            throw new InvalidOperationException("Patient must be checked in first.");
        Status = AppointmentStatus.InProgress;
    }

    public void Complete(DateTime completedAtUtc)
    {
        if (Status != AppointmentStatus.InProgress)
            throw new InvalidOperationException("Appointment must be in progress to complete.");
        Status = AppointmentStatus.Completed;
        CompletedAtUtc = completedAtUtc;
    }

    public void MarkNoShow()
    {
        if (Status != AppointmentStatus.Scheduled)
            throw new InvalidOperationException($"Cannot mark no-show in status {Status}.");
        Status = AppointmentStatus.NoShow;
    }
}
