using Dialysis.BuildingBlocks.Fhir.Serialization;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Snapshot;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Validation.Authoring;

/// <summary>Outcome of verifying an authored artifact's correctness.</summary>
public sealed record FhirArtifactVerification
{
    /// <summary>Outcome of verifying an authored artifact's correctness.</summary>
    public FhirArtifactVerification(bool IsValid, OperationOutcome Outcome)
    {
        this.IsValid = IsValid;
        this.Outcome = Outcome;
    }
    public bool IsValid { get; init; }
    public OperationOutcome Outcome { get; init; }
    public void Deconstruct(out bool IsValid, out OperationOutcome Outcome)
    {
        IsValid = this.IsValid;
        Outcome = this.Outcome;
    }
}

/// <summary>
/// Independently verifies that an authored profile / IG is correct: required metadata is present,
/// the differential snapshots cleanly against the resolved base definitions (the strongest
/// computability proof), and the artifact round-trips through the FHIR serializer.
/// </summary>
public interface IFhirArtifactVerifier
{
    Task<FhirArtifactVerification> VerifyProfileAsync(
        StructureDefinition profile, CancellationToken cancellationToken);

    Task<FhirArtifactVerification> VerifyImplementationGuideAsync(
        ImplementationGuide guide,
        IEnumerable<StructureDefinition> profiles,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="IFhirArtifactVerifier" />
public sealed class FhirArtifactVerifier : IFhirArtifactVerifier
{
    private readonly IFhirConformanceRegistry _registry;
    private readonly FhirJsonSerializerProvider _serializer;
    /// <inheritdoc cref="IFhirArtifactVerifier" />
    public FhirArtifactVerifier(IFhirConformanceRegistry registry,
        FhirJsonSerializerProvider serializer)
    {
        _registry = registry;
        _serializer = serializer;
    }
    public async Task<FhirArtifactVerification> VerifyProfileAsync(
        StructureDefinition profile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var outcome = new OperationOutcome();

        RequireMetadata(outcome, profile);
        await VerifySnapshotAsync(outcome, profile).ConfigureAwait(false);
        VerifyRoundTrip(outcome, profile, $"StructureDefinition '{profile.Url}'");

        return Finalize(outcome);
    }

    public async Task<FhirArtifactVerification> VerifyImplementationGuideAsync(
        ImplementationGuide guide,
        IEnumerable<StructureDefinition> profiles,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(guide);
        var profileList = profiles as IReadOnlyList<StructureDefinition> ?? [.. profiles];
        var outcome = new OperationOutcome();

        if (string.IsNullOrWhiteSpace(guide.Url))
            Add(outcome, OperationOutcome.IssueSeverity.Error, "ImplementationGuide.url is required.");
        if (string.IsNullOrWhiteSpace(guide.Name))
            Add(outcome, OperationOutcome.IssueSeverity.Error, "ImplementationGuide.name is required.");
        if (string.IsNullOrWhiteSpace(guide.PackageId))
            Add(outcome, OperationOutcome.IssueSeverity.Error, "ImplementationGuide.packageId is required.");
        if (guide.FhirVersion is null || !guide.FhirVersion.Any())
            Add(outcome, OperationOutcome.IssueSeverity.Error, "ImplementationGuide.fhirVersion is required.");

        foreach (var profile in profileList)
        {
            var sub = await VerifyProfileAsync(profile, cancellationToken).ConfigureAwait(false);
            foreach (var issue in sub.Outcome.Issue)
                outcome.Issue.Add(issue);
        }

        // definition.resource references must point at profiles bundled with the IG.
        var profileIds = profileList.Select(p => $"StructureDefinition/{p.Id}").ToHashSet(StringComparer.Ordinal);
        foreach (var res in guide.Definition?.Resource ?? [])
        {
            var reference = res.Reference?.Reference;
            if (reference is not null && !profileIds.Contains(reference))
            {
                Add(outcome, OperationOutcome.IssueSeverity.Error,
                    $"IG definition references '{reference}' which is not among the authored profiles.");
            }
        }

        // External dependencies are advisory: warn (not fail) when offline / unresolved.
        foreach (var dep in guide.DependsOn ?? [])
        {
            if (string.IsNullOrWhiteSpace(dep.Uri))
            {
                Add(outcome, OperationOutcome.IssueSeverity.Error, "ImplementationGuide.dependsOn.uri is required.");
                continue;
            }

            var resolved = await _registry.TryResolveByCanonicalUriAsync(dep.Uri).ConfigureAwait(false);
            if (!resolved.Success)
            {
                Add(outcome, OperationOutcome.IssueSeverity.Warning,
                    $"Dependency '{dep.Uri}' could not be resolved from the loaded conformance sources.");
            }
        }

        VerifyRoundTrip(outcome, guide, $"ImplementationGuide '{guide.Url}'");
        return Finalize(outcome);
    }

    private static void RequireMetadata(OperationOutcome outcome, StructureDefinition sd)
    {
        if (string.IsNullOrWhiteSpace(sd.Url))
            Add(outcome, OperationOutcome.IssueSeverity.Error, "StructureDefinition.url is required.");
        if (string.IsNullOrWhiteSpace(sd.Name))
            Add(outcome, OperationOutcome.IssueSeverity.Error, "StructureDefinition.name is required.");
        if (string.IsNullOrWhiteSpace(sd.Type))
            Add(outcome, OperationOutcome.IssueSeverity.Error, "StructureDefinition.type is required.");
        if (string.IsNullOrWhiteSpace(sd.BaseDefinition))
            Add(outcome, OperationOutcome.IssueSeverity.Error, "StructureDefinition.baseDefinition is required.");
        if (sd.FhirVersion is null)
            Add(outcome, OperationOutcome.IssueSeverity.Error, "StructureDefinition.fhirVersion is required.");
        if ((sd.Differential?.Element.Count ?? 0) == 0)
            Add(outcome, OperationOutcome.IssueSeverity.Error, "StructureDefinition.differential has no elements.");
        else if (!string.Equals(sd.Differential!.Element[0].Path, sd.Type, StringComparison.Ordinal))
            Add(outcome, OperationOutcome.IssueSeverity.Warning,
                $"Differential root '{sd.Differential.Element[0].Path}' does not match type '{sd.Type}'.");
    }

    private async Task VerifySnapshotAsync(OperationOutcome outcome, StructureDefinition sd)
    {
        // Re-generate onto a clone so a malformed differential surfaces here independently of how
        // the artifact was built. A clean snapshot is the strongest proof the profile is computable.
        var clone = (StructureDefinition)sd.DeepCopy();
        var generator = new SnapshotGenerator(_registry, SnapshotGeneratorSettings.CreateDefault());

        try
        {
            await generator.UpdateAsync(clone).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Add(outcome, OperationOutcome.IssueSeverity.Fatal,
                $"Snapshot generation threw for '{sd.Url}': {ex.Message}");
            return;
        }

        foreach (var issue in generator.Outcome?.Issue ?? [])
            outcome.Issue.Add(issue);

        if ((clone.Snapshot?.Element.Count ?? 0) == 0)
        {
            Add(outcome, OperationOutcome.IssueSeverity.Error,
                $"Snapshot generation produced no elements for '{sd.Url}' " +
                "(base definition could not be resolved or the differential is empty).");
        }
        else
        {
            sd.Snapshot = clone.Snapshot;
            Add(outcome, OperationOutcome.IssueSeverity.Information,
                $"Snapshot generated with {clone.Snapshot!.Element.Count} elements.");
        }
    }

    private void VerifyRoundTrip(OperationOutcome outcome, Resource resource, string label)
    {
        try
        {
            var json = FhirJsonSerializerProvider.Serialize(resource);
            _ = _serializer.Parse<Resource>(json);
        }
        catch (Exception ex)
        {
            Add(outcome, OperationOutcome.IssueSeverity.Error,
                $"{label} failed FHIR JSON round-trip: {ex.Message}");
        }
    }

    private static void Add(OperationOutcome outcome, OperationOutcome.IssueSeverity severity, string message)
        => outcome.Issue.Add(new OperationOutcome.IssueComponent
        {
            Severity = severity,
            Code = severity is OperationOutcome.IssueSeverity.Information
                ? OperationOutcome.IssueType.Informational
                : OperationOutcome.IssueType.Invalid,
            Diagnostics = message,
        });

    private static FhirArtifactVerification Finalize(OperationOutcome outcome)
    {
        var isValid = !outcome.Issue.Any(i =>
            i.Severity is OperationOutcome.IssueSeverity.Error or OperationOutcome.IssueSeverity.Fatal);
        return new FhirArtifactVerification(isValid, outcome);
    }
}
