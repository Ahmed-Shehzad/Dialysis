using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents.Scheduling;

/// <summary>
/// Emitted when an appointment is booked in HIS scheduling. EHR's scheduling sub-context consumes via outbox
/// to keep the longitudinal appointment record aligned with the operational booking.
/// </summary>
public sealed record AppointmentBookedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid AppointmentId,
    Guid PatientId,
    Guid ProviderId,
    DateTime SlotStartUtc,
    DateTime SlotEndUtc) : IIntegrationEvent;
