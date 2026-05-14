namespace Dialysis.BuildingBlocks.Fhir;

/// <summary>
/// Default permit-all consent gate. Used in development and by modules without patient consent flows.
/// Production deployments should register a module-specific <see cref="IFhirConsentGate"/>.
/// </summary>
public sealed class NoOpConsentGate : IFhirConsentGate
{
    public ValueTask<FhirConsentDecision> EvaluateAsync(FhirConsentContext context, CancellationToken cancellationToken)
        => new(FhirConsentDecision.Permit());
}
