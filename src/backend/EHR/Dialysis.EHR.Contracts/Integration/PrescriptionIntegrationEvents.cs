using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

public sealed record PrescriptionOrderedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid PrescriptionId,
    Guid PatientId,
    Guid EncounterId,
    Guid PrescribingProviderId,
    string MedicationRxnormCode,
    string MedicationDisplay,
    string DoseText,
    string FrequencyText,
    int QuantityDispensed,
    int RefillsAuthorized,
    string PharmacyNcpdpId,
    string TransmissionFormat) : IIntegrationEvent;

public sealed record PrescriptionCancelledIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid PrescriptionId,
    Guid PatientId,
    string ReasonCode) : IIntegrationEvent;

public sealed record PrescriptionAcknowledgedByPharmacyIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid PrescriptionId,
    string PharmacyNcpdpId,
    string AcknowledgementCode,
    string? Notes) : IIntegrationEvent;
