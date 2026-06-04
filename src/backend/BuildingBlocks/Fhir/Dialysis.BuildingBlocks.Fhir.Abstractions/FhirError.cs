namespace Dialysis.BuildingBlocks.Fhir;

public sealed record FhirError
{
    public FhirError(string Code, string Diagnostics, FhirIssueSeverity Severity = FhirIssueSeverity.Error)
    {
        this.Code = Code;
        this.Diagnostics = Diagnostics;
        this.Severity = Severity;
    }
    public string Code { get; init; }
    public string Diagnostics { get; init; }
    public FhirIssueSeverity Severity { get; init; }
    public void Deconstruct(out string Code, out string Diagnostics, out FhirIssueSeverity Severity)
    {
        Code = this.Code;
        Diagnostics = this.Diagnostics;
        Severity = this.Severity;
    }
}
