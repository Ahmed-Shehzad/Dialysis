using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents;

public sealed record MedicationOrderPlacedIntegrationEvent(
    Guid OrderId,
    Guid PatientId,
    string MedicationCode,
    DateTime OrderedAtUtc)
    : IntegrationEvent;
