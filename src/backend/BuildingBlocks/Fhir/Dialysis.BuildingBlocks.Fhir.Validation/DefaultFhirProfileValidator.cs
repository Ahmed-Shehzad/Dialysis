using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Validation;

/// <summary>
/// Default <see cref="IFhirProfileValidator"/> that consults the <see cref="FhirProfileMap"/>.
/// v1 ships a permissive structural check (returns success if no profile is bound for the type);
/// full Firely structural validation against US Core / USCDI / CH Core <c>.tgz</c> packages is wired
/// by hosts that supply a real validator implementation. This default keeps the building block
/// usable without dragging the heavy snapshot generator into every build.
/// </summary>
public sealed class DefaultFhirProfileValidator : IFhirProfileValidator
{
    private readonly FhirProfileMap _profileMap;
    /// <summary>
    /// Default <see cref="IFhirProfileValidator"/> that consults the <see cref="FhirProfileMap"/>.
    /// v1 ships a permissive structural check (returns success if no profile is bound for the type);
    /// full Firely structural validation against US Core / USCDI / CH Core <c>.tgz</c> packages is wired
    /// by hosts that supply a real validator implementation. This default keeps the building block
    /// usable without dragging the heavy snapshot generator into every build.
    /// </summary>
    public DefaultFhirProfileValidator(FhirProfileMap profileMap) => _profileMap = profileMap;
    public ValueTask<FhirProfileValidationResult> ValidateAsync(Resource resource, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);
        var profiles = _profileMap.GetProfilesFor(resource.TypeName);
        if (profiles.Count == 0 || _profileMap.Mode == FhirProfileEnforcementMode.Off)
            return new ValueTask<FhirProfileValidationResult>(new FhirProfileValidationResult(IsValid: true, new OperationOutcome()));

        // Skeleton — profile-driven validation is wired by the host when a real validator is provided.
        // The returned outcome lists the bound profiles as informational entries so callers can surface
        // them even before structural validation is enabled.
        var outcome = new OperationOutcome();
        foreach (var profile in profiles)
        {
            outcome.Issue.Add(new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Information,
                Code = OperationOutcome.IssueType.Informational,
                Diagnostics = $"profile {profile} is bound for {resource.TypeName}",
            });
        }
        return new ValueTask<FhirProfileValidationResult>(new FhirProfileValidationResult(IsValid: true, outcome));
    }
}
