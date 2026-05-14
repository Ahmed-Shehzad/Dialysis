namespace Dialysis.BuildingBlocks.Fhir.Testing;

public sealed class FakeConsentGate(bool permit = true, string? denialReason = null) : IFhirConsentGate
{
    public ValueTask<FhirConsentDecision> EvaluateAsync(FhirConsentContext context, CancellationToken cancellationToken)
        => new(permit ? FhirConsentDecision.Permit() : FhirConsentDecision.Deny(denialReason ?? "denied"));
}
