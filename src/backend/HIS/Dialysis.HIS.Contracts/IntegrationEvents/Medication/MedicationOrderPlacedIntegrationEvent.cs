using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents.Medication;

/// <summary>
/// Emitted when a medication order is placed in HIS medication. Pharmacy modules consume to dispense.
/// </summary>
public sealed record MedicationOrderPlacedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid OrderId,
    Guid PatientId,
    string DrugCode,
    string Dosage,
    DateTime PlacedAtUtc) : IIntegrationEvent;
