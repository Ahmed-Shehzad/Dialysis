namespace Dialysis.BuildingBlocks.Fhir;

public interface IFhirConsentGate
{
    ValueTask<FhirConsentDecision> EvaluateAsync(FhirConsentContext context, CancellationToken cancellationToken);
}

public sealed record FhirConsentContext(
    string ResourceType,
    string? ResourceId,
    string? PatientId,
    string? RequestorId,
    string Purpose = "treatment");

public sealed record FhirConsentDecision(bool Permitted, string? Reason = null)
{
    public static FhirConsentDecision Permit() => new(true);
    public static FhirConsentDecision Deny(string reason) => new(false, reason);
}
