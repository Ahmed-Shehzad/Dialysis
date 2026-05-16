using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Validation.Authoring;

/// <summary>Result of an on-demand authoring request: the artifact plus its verification outcome.</summary>
public sealed record AuthoredProfileResult(
    StructureDefinition Profile, FhirArtifactVerification Verification, bool Published);

/// <summary>Result of an on-demand IG authoring request.</summary>
public sealed record AuthoredImplementationGuideResult(
    ImplementationGuide Guide,
    IReadOnlyList<StructureDefinition> Profiles,
    FhirArtifactVerification Verification,
    bool Published);

/// <summary>
/// One-call orchestration for the on-the-fly authoring flow: build → verify → (publish into the
/// conformance registry only when correct). Invalid artifacts are returned with their
/// <see cref="OperationOutcome"/> but never published, so a malformed profile can't poison
/// downstream validation.
/// </summary>
public interface IFhirArtifactAuthoringService
{
    Task<AuthoredProfileResult> AuthorProfileAsync(
        FhirProfileSpec spec, CancellationToken cancellationToken);

    Task<AuthoredImplementationGuideResult> AuthorImplementationGuideAsync(
        FhirImplementationGuideSpec spec, CancellationToken cancellationToken);
}

/// <inheritdoc cref="IFhirArtifactAuthoringService" />
public sealed class FhirArtifactAuthoringService(
    IFhirProfileFactory profileFactory,
    IFhirImplementationGuideFactory guideFactory,
    IFhirArtifactVerifier verifier,
    IFhirConformanceRegistry registry) : IFhirArtifactAuthoringService
{
    public async Task<AuthoredProfileResult> AuthorProfileAsync(
        FhirProfileSpec spec, CancellationToken cancellationToken)
    {
        var profile = await profileFactory.BuildAsync(spec, cancellationToken).ConfigureAwait(false);
        var verification = await verifier.VerifyProfileAsync(profile, cancellationToken).ConfigureAwait(false);

        if (verification.IsValid)
            registry.Register(profile);

        return new AuthoredProfileResult(profile, verification, verification.IsValid);
    }

    public async Task<AuthoredImplementationGuideResult> AuthorImplementationGuideAsync(
        FhirImplementationGuideSpec spec, CancellationToken cancellationToken)
    {
        var authored = await guideFactory.BuildAsync(spec, cancellationToken).ConfigureAwait(false);
        var verification = await verifier
            .VerifyImplementationGuideAsync(authored.Guide, authored.Profiles, cancellationToken)
            .ConfigureAwait(false);

        if (verification.IsValid)
        {
            foreach (var profile in authored.Profiles)
                registry.Register(profile);
            registry.Register(authored.Guide);
        }

        return new AuthoredImplementationGuideResult(
            authored.Guide, authored.Profiles, verification, verification.IsValid);
    }
}
