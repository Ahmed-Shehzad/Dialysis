using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents;

public sealed record MedicationOrderDiscontinuedIntegrationEvent(
    Guid OrderId,
    Guid PatientId,
    string MedicationCode,
    DateTime DiscontinuedAtUtc)
    : IntegrationEvent;
