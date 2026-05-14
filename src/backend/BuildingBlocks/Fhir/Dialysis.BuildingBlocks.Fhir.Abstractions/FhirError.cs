namespace Dialysis.BuildingBlocks.Fhir;

public sealed record FhirError(string Code, string Diagnostics, FhirIssueSeverity Severity = FhirIssueSeverity.Error);
