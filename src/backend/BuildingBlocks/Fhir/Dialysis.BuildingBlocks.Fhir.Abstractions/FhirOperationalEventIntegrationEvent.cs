namespace Dialysis.BuildingBlocks.Fhir;

public sealed record FhirOperationalEventIntegrationEvent(
    string Operation,
    string ResourceType,
    string? PatientReference,
    FhirIssueSeverity Severity,
    string? Code,
    string? Message,
    string CorrelationId,
    DateTimeOffset OccurredAt,
    int SchemaVersion = 1);
