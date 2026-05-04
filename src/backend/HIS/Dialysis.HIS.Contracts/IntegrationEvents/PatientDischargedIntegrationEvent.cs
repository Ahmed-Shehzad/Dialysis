using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents;

public sealed record PatientDischargedIntegrationEvent(
    Guid PatientId,
    DateTime DischargedAtUtc)
    : IntegrationEvent;
