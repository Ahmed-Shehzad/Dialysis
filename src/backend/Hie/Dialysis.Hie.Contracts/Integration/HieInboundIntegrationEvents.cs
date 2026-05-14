using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.Hie.Contracts.Integration;

/// <summary>
/// Emitted when an external partner POSTs a FHIR Patient resource that the HIE has accepted into its
/// MPI. EHR consumes this to enrich its own patient index without becoming directly coupled to FHIR.
/// </summary>
public sealed record ExternalPatientReferenceIngestedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    string PartnerId,
    string ExternalLogicalId,
    string? MedicalRecordNumber,
    string? FamilyName,
    string? GivenName,
    DateOnly? DateOfBirth,
    string? SexAtBirthCode) : IIntegrationEvent;

/// <summary>Emitted on accepted inbound FHIR <c>Encounter</c>.</summary>
public sealed record ExternalEncounterIngestedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    string PartnerId,
    string ExternalLogicalId,
    string? PatientExternalLogicalId,
    DateTime? PeriodStartUtc,
    DateTime? PeriodEndUtc,
    string? ClassCode,
    string? ReasonCode) : IIntegrationEvent;

/// <summary>Emitted on accepted inbound FHIR <c>Observation</c>/<c>DiagnosticReport</c> with lab data.</summary>
public sealed record ExternalLabResultIngestedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    string PartnerId,
    string ExternalLogicalId,
    string? PatientExternalLogicalId,
    string LoincCode,
    string DisplayName,
    string? ValueQuantity,
    string? Unit,
    DateTime? ObservedAtUtc) : IIntegrationEvent;

/// <summary>Emitted on accepted inbound FHIR <c>Procedure</c> describing a dialysis session.</summary>
public sealed record ExternalDialysisSessionIngestedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    string PartnerId,
    string ExternalLogicalId,
    string? PatientExternalLogicalId,
    DateTime? PerformedStartUtc,
    DateTime? PerformedEndUtc,
    string? OutcomeCode) : IIntegrationEvent;
