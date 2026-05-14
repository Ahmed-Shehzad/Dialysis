using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIE.Contracts.Integration;

/// <summary>
/// Emitted when a FHIR resource bundle has been successfully delivered to an external partner endpoint.
/// Downstream subscribers (audit, billing, analytics) can react without coupling to the HIE module.
/// </summary>
public sealed record FhirResourceDeliveredIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid OutboundBundleId,
    Guid PatientId,
    string ResourceType,
    string LogicalId,
    string PartnerId,
    DateTime DeliveredAtUtc) : IIntegrationEvent;

/// <summary>
/// Emitted when a FHIR resource bundle has failed delivery after exhausting the retry policy.
/// Operations team should investigate (peer endpoint outage, schema rejection, consent revocation).
/// </summary>
public sealed record FhirResourceDeliveryFailedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid OutboundBundleId,
    Guid PatientId,
    string ResourceType,
    string PartnerId,
    int Attempts,
    string FailureReason) : IIntegrationEvent;
