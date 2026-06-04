namespace Dialysis.BuildingBlocks.Fhir;

public sealed record FhirSearchRequest
{
    public FhirSearchRequest(string ResourceType,
        IReadOnlyDictionary<string, string[]> Parameters,
        int? Count = null,
        string? ContinuationToken = null)
    {
        this.ResourceType = ResourceType;
        this.Parameters = Parameters;
        this.Count = Count;
        this.ContinuationToken = ContinuationToken;
    }
    public string ResourceType { get; init; }
    public IReadOnlyDictionary<string, string[]> Parameters { get; init; }
    public int? Count { get; init; }
    public string? ContinuationToken { get; init; }
    public void Deconstruct(out string ResourceType, out IReadOnlyDictionary<string, string[]> Parameters, out int? Count, out string? ContinuationToken)
    {
        ResourceType = this.ResourceType;
        Parameters = this.Parameters;
        Count = this.Count;
        ContinuationToken = this.ContinuationToken;
    }
}
