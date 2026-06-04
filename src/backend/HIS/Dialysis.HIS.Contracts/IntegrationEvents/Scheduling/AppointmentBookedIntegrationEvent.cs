using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents.Scheduling;

/// <summary>
/// Emitted when an appointment is booked in HIS scheduling. EHR's scheduling sub-context consumes via outbox
/// to keep the longitudinal appointment record aligned with the operational booking.
/// </summary>
public sealed record AppointmentBookedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Emitted when an appointment is booked in HIS scheduling. EHR's scheduling sub-context consumes via outbox
    /// to keep the longitudinal appointment record aligned with the operational booking.
    /// </summary>
    public AppointmentBookedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid AppointmentId,
        Guid PatientId,
        Guid ProviderId,
        DateTime SlotStartUtc,
        DateTime SlotEndUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.AppointmentId = AppointmentId;
        this.PatientId = PatientId;
        this.ProviderId = ProviderId;
        this.SlotStartUtc = SlotStartUtc;
        this.SlotEndUtc = SlotEndUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid AppointmentId { get; init; }
    public Guid PatientId { get; init; }
    public Guid ProviderId { get; init; }
    public DateTime SlotStartUtc { get; init; }
    public DateTime SlotEndUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid AppointmentId, out Guid PatientId, out Guid ProviderId, out DateTime SlotStartUtc, out DateTime SlotEndUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        AppointmentId = this.AppointmentId;
        PatientId = this.PatientId;
        ProviderId = this.ProviderId;
        SlotStartUtc = this.SlotStartUtc;
        SlotEndUtc = this.SlotEndUtc;
    }
}
