using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.HIS.Contracts.IntegrationEvents.Scheduling;
using Dialysis.HIS.Scheduling.Domain.ValueObjects;

namespace Dialysis.HIS.Scheduling.Domain;

public sealed class Appointment : AggregateRoot<Guid>
{
    public Guid PatientId { get; private set; }
    public Guid ProviderId { get; private set; }
    public AppointmentSlot Slot { get; private set; } = null!;
    public string StatusCode { get; private set; } = "Booked";
    public DateTime BookedAtUtc { get; private set; }

    private Appointment() { }
    private Appointment(Guid id) : base(id) { }

    public static Appointment Book(Guid patientId, Guid providerId, AppointmentSlot slot, DateTime nowUtc)
    {
        if (patientId == Guid.Empty)
            throw new DomainException("PatientId cannot be empty.");
        if (providerId == Guid.Empty)
            throw new DomainException("ProviderId cannot be empty.");
        ArgumentNullException.ThrowIfNull(slot);

        var appt = new Appointment(Guid.CreateVersion7())
        {
            PatientId = patientId,
            ProviderId = providerId,
            Slot = slot,
            StatusCode = "Booked",
            BookedAtUtc = nowUtc,
        };

        appt.RaiseIntegrationEvent(new AppointmentBookedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: nowUtc,
            SchemaVersion: 1,
            AppointmentId: appt.Id,
            PatientId: patientId,
            ProviderId: providerId,
            SlotStartUtc: slot.StartUtc,
            SlotEndUtc: slot.EndUtc));

        return appt;
    }

    public void Cancel(DateTime nowUtc)
    {
        if (StatusCode == "Cancelled")
            throw new DomainException("Appointment is already cancelled.");
        StatusCode = "Cancelled";
    }
}
