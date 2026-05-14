namespace Dialysis.BuildingBlocks.Fhir;

public sealed record FhirSearchRequest(
    string ResourceType,
    IReadOnlyDictionary<string, string[]> Parameters,
    int? Count = null,
    string? ContinuationToken = null);
