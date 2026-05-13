using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

public sealed record LabOrderPlacedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid LabOrderId,
    Guid PatientId,
    Guid EncounterId,
    Guid OrderingProviderId,
    string LabFacilityCode,
    IReadOnlyList<string> LoincPanelCodes,
    string TransmissionFormat) : IIntegrationEvent;

public sealed record LabOrderCancelledIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid LabOrderId,
    Guid PatientId,
    string ReasonCode) : IIntegrationEvent;

public sealed record LabResultReceivedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid LabResultId,
    Guid LabOrderId,
    Guid PatientId,
    string LoincCode,
    string ValueText,
    string? UnitCode,
    string? ReferenceRangeText,
    string AbnormalFlag,
    DateTime ObservedAtUtc) : IIntegrationEvent;
