namespace Dialysis.BuildingBlocks.Fhir;

public interface IFhirConsentGate
{
    ValueTask<FhirConsentDecision> EvaluateAsync(FhirConsentContext context, CancellationToken cancellationToken);
}

public sealed record FhirConsentContext
{
    public FhirConsentContext(string ResourceType,
        string? ResourceId,
        string? PatientId,
        string? RequestorId,
        string Purpose = "treatment")
    {
        this.ResourceType = ResourceType;
        this.ResourceId = ResourceId;
        this.PatientId = PatientId;
        this.RequestorId = RequestorId;
        this.Purpose = Purpose;
    }
    public string ResourceType { get; init; }
    public string? ResourceId { get; init; }
    public string? PatientId { get; init; }
    public string? RequestorId { get; init; }
    public string Purpose { get; init; }
    public void Deconstruct(out string ResourceType, out string? ResourceId, out string? PatientId, out string? RequestorId, out string Purpose)
    {
        ResourceType = this.ResourceType;
        ResourceId = this.ResourceId;
        PatientId = this.PatientId;
        RequestorId = this.RequestorId;
        Purpose = this.Purpose;
    }
}

public sealed record FhirConsentDecision
{
    public FhirConsentDecision(bool Permitted, string? Reason = null)
    {
        this.Permitted = Permitted;
        this.Reason = Reason;
    }
    public static FhirConsentDecision Permit() => new(true);
    public static FhirConsentDecision Deny(string reason) => new(false, reason);
    public bool Permitted { get; init; }
    public string? Reason { get; init; }
    public void Deconstruct(out bool Permitted, out string? Reason)
    {
        Permitted = this.Permitted;
        Reason = this.Reason;
    }
}
