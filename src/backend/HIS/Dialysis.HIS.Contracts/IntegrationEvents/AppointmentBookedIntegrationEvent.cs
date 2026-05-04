using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents;

public sealed record AppointmentBookedIntegrationEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid ResourceId,
    DateTime StartUtc,
    DateTime EndUtc)
    : IntegrationEvent;
