using Dialysis.BuildingBlocks.Fhir.Validation;
using Dialysis.BuildingBlocks.Fhir.Validation.Authoring;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.Fhir.Tests.Authoring;

/// <summary>
/// Exercises the on-demand profile / Implementation Guide authoring pipeline against the real
/// Firely snapshot generator + bundled FHIR R4 core specification — proving artifacts are built,
/// their correctness verified, and only valid ones published into the conformance registry.
/// </summary>
public sealed class FhirArtifactAuthoringTests
{
    private static (IFhirArtifactAuthoringService Authoring, IFhirConformanceRegistry Registry) Build()
    {
        var services = new ServiceCollection();
        services.AddFhirArtifactAuthoring();
        var sp = services.BuildServiceProvider();
        return (sp.GetRequiredService<IFhirArtifactAuthoringService>(),
                sp.GetRequiredService<IFhirConformanceRegistry>());
    }

    [Fact]
    public async Task Authors_Profile_Generates_Snapshot_And_Publishes_When_Valid_Async()
    {
        var (authoring, registry) = Build();
        var spec = FhirProfileSpec.For(
                "Patient",
                "https://dialysis.local/fhir/StructureDefinition/dialysis-patient",
                "DialysisPatient")
            .Title("Dialysis Patient")
            .Description("Patient profile requiring an MRN identifier and a supported name.")
            .Require("Patient.identifier")
            .MustSupport("Patient.name")
            .Build();

        var result = await authoring.AuthorProfileAsync(spec, CancellationToken.None);

        result.Verification.IsValid.ShouldBeTrue(
            string.Join("; ", result.Verification.Outcome.Issue.Select(i => i.Diagnostics)));
        result.Published.ShouldBeTrue();
        result.Profile.Snapshot.ShouldNotBeNull();
        result.Profile.Snapshot.Element.Count.ShouldBeGreaterThan(0);

        // Resolvable on the fly for downstream validation / profile layering.
        registry.TryGet(spec.Url, out var resolved).ShouldBeTrue();
        resolved.ShouldBeOfType<Hl7.Fhir.Model.StructureDefinition>();
        var byCanonical = await registry.TryResolveByCanonicalUriAsync(spec.Url);
        byCanonical.Value.ShouldNotBeNull();
    }

    [Fact]
    public async Task Invalid_Base_Definition_Fails_Verification_And_Is_Not_Published_Async()
    {
        var (authoring, registry) = Build();
        var spec = FhirProfileSpec.For(
                "Patient",
                "https://dialysis.local/fhir/StructureDefinition/broken",
                "BrokenProfile")
            .DerivedFrom("http://example.org/StructureDefinition/DoesNotExist")
            .Require("Patient.identifier")
            .Build();

        var result = await authoring.AuthorProfileAsync(spec, CancellationToken.None);

        result.Verification.IsValid.ShouldBeFalse();
        result.Published.ShouldBeFalse();
        result.Verification.Outcome.Issue
            .ShouldContain(i => i.Severity == Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Error
                || i.Severity == Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Fatal);
        registry.TryGet(spec.Url, out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Constraint_Path_Not_Rooted_At_Type_Throws_Async()
    {
        var (authoring, _) = Build();
        var spec = FhirProfileSpec.For(
                "Patient",
                "https://dialysis.local/fhir/StructureDefinition/misrooted",
                "MisRooted")
            .Constrain(new FhirElementConstraint { Path = "Observation.code", Min = 1 })
            .Build();

        await Should.ThrowAsync<ArgumentException>(
            () => authoring.AuthorProfileAsync(spec, CancellationToken.None));
    }

    [Fact]
    public async Task Authors_Implementation_Guide_With_Profiles_And_Dependency_Async()
    {
        var (authoring, registry) = Build();
        var profile = FhirProfileSpec.For(
                "Observation",
                "https://dialysis.local/fhir/StructureDefinition/dialysis-lab",
                "DialysisLabObservation")
            .Require("Observation.code")
            .Require("Observation.subject")
            .Build();

        var igSpec = new FhirImplementationGuideSpec
        {
            PackageId = "dialysis.fhir.core",
            Url = "https://dialysis.local/fhir/ImplementationGuide/dialysis-core",
            Name = "DialysisCoreIG",
            Title = "Dialysis Core Implementation Guide",
            Version = "0.1.0",
            Profiles = [profile],
            DependsOn =
            [
                new FhirIgDependency
                {
                    Uri = "http://hl7.org/fhir/us/core/ImplementationGuide/hl7.fhir.us.core",
                    PackageId = "hl7.fhir.us.core",
                    Version = "6.1.0",
                },
            ],
        };

        var result = await authoring.AuthorImplementationGuideAsync(igSpec, CancellationToken.None);

        result.Verification.IsValid.ShouldBeTrue(
            string.Join("; ", result.Verification.Outcome.Issue.Select(i => i.Diagnostics)));
        result.Published.ShouldBeTrue();
        result.Profiles.Count.ShouldBe(1);
        result.Guide.Definition!.Resource.Count.ShouldBe(1);
        result.Guide.Global.Count.ShouldBe(1);

        // The unresolved external dependency is advisory (a Warning), never a hard failure.
        result.Verification.Outcome.Issue
            .ShouldContain(i => i.Severity == Hl7.Fhir.Model.OperationOutcome.IssueSeverity.Warning
                && i.Diagnostics!.Contains("us/core", StringComparison.Ordinal));

        registry.ImplementationGuides.ShouldContain(g => g.Url == igSpec.Url);
        registry.Profiles.ShouldContain(p => p.Url == profile.Url);
    }
}
