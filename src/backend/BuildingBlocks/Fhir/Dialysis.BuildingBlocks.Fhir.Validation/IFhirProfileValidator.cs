using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Validation;

public enum FhirProfileEnforcementMode
{
    Off,
    Warn,
    Strict,
}

public sealed record FhirProfileValidationResult(bool IsValid, OperationOutcome Outcome);

public interface IFhirProfileValidator
{
    /// <summary>
    /// Validates <paramref name="resource"/> against the profile(s) bound for its type in the
    /// <see cref="FhirProfileMap"/>. Returns an <see cref="OperationOutcome"/> describing any issues.
    /// </summary>
    ValueTask<FhirProfileValidationResult> ValidateAsync(Resource resource, CancellationToken cancellationToken);
}

public sealed class FhirProfileMap
{
    private readonly Dictionary<string, List<string>> _bindings = new(StringComparer.Ordinal);

    public FhirProfileEnforcementMode Mode { get; set; } = FhirProfileEnforcementMode.Warn;

    public FhirProfileMap Require<TResource>(string profileUrl) where TResource : Resource, new()
    {
        var typeName = new TResource().TypeName;
        if (!_bindings.TryGetValue(typeName, out var list))
            _bindings[typeName] = list = [];
        if (!list.Contains(profileUrl, StringComparer.Ordinal))
            list.Add(profileUrl);
        return this;
    }

    public IReadOnlyList<string> GetProfilesFor(string resourceType)
        => _bindings.TryGetValue(resourceType, out var list) ? list : Array.Empty<string>();
}
