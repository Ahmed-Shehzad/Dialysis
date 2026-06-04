namespace Dialysis.BuildingBlocks.Fhir.Testing;

public sealed class FakeConsentGate : IFhirConsentGate
{
    private readonly bool _permit;
    private readonly string? _denialReason;
    public FakeConsentGate(bool permit = true, string? denialReason = null)
    {
        _permit = permit;
        _denialReason = denialReason;
    }
    public ValueTask<FhirConsentDecision> EvaluateAsync(FhirConsentContext context, CancellationToken cancellationToken)
        => new(_permit ? FhirConsentDecision.Permit() : FhirConsentDecision.Deny(_denialReason ?? "denied"));
}
