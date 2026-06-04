using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Validation.Authoring;

/// <summary>Result of an on-demand authoring request: the artifact plus its verification outcome.</summary>
public sealed record AuthoredProfileResult
{
    /// <summary>Result of an on-demand authoring request: the artifact plus its verification outcome.</summary>
    public AuthoredProfileResult(StructureDefinition Profile, FhirArtifactVerification Verification, bool Published)
    {
        this.Profile = Profile;
        this.Verification = Verification;
        this.Published = Published;
    }
    public StructureDefinition Profile { get; init; }
    public FhirArtifactVerification Verification { get; init; }
    public bool Published { get; init; }
    public void Deconstruct(out StructureDefinition Profile, out FhirArtifactVerification Verification, out bool Published)
    {
        Profile = this.Profile;
        Verification = this.Verification;
        Published = this.Published;
    }
}

/// <summary>Result of an on-demand IG authoring request.</summary>
public sealed record AuthoredImplementationGuideResult
{
    /// <summary>Result of an on-demand IG authoring request.</summary>
    public AuthoredImplementationGuideResult(ImplementationGuide Guide,
        IReadOnlyList<StructureDefinition> Profiles,
        FhirArtifactVerification Verification,
        bool Published)
    {
        this.Guide = Guide;
        this.Profiles = Profiles;
        this.Verification = Verification;
        this.Published = Published;
    }
    public ImplementationGuide Guide { get; init; }
    public IReadOnlyList<StructureDefinition> Profiles { get; init; }
    public FhirArtifactVerification Verification { get; init; }
    public bool Published { get; init; }
    public void Deconstruct(out ImplementationGuide Guide, out IReadOnlyList<StructureDefinition> Profiles, out FhirArtifactVerification Verification, out bool Published)
    {
        Guide = this.Guide;
        Profiles = this.Profiles;
        Verification = this.Verification;
        Published = this.Published;
    }
}

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
public sealed class FhirArtifactAuthoringService : IFhirArtifactAuthoringService
{
    private readonly IFhirProfileFactory _profileFactory;
    private readonly IFhirImplementationGuideFactory _guideFactory;
    private readonly IFhirArtifactVerifier _verifier;
    private readonly IFhirConformanceRegistry _registry;
    /// <inheritdoc cref="IFhirArtifactAuthoringService" />
    public FhirArtifactAuthoringService(IFhirProfileFactory profileFactory,
        IFhirImplementationGuideFactory guideFactory,
        IFhirArtifactVerifier verifier,
        IFhirConformanceRegistry registry)
    {
        _profileFactory = profileFactory;
        _guideFactory = guideFactory;
        _verifier = verifier;
        _registry = registry;
    }
    public async Task<AuthoredProfileResult> AuthorProfileAsync(
        FhirProfileSpec spec, CancellationToken cancellationToken)
    {
        var profile = await _profileFactory.BuildAsync(spec, cancellationToken).ConfigureAwait(false);
        var verification = await _verifier.VerifyProfileAsync(profile, cancellationToken).ConfigureAwait(false);

        if (verification.IsValid)
            _registry.Register(profile);

        return new AuthoredProfileResult(profile, verification, verification.IsValid);
    }

    public async Task<AuthoredImplementationGuideResult> AuthorImplementationGuideAsync(
        FhirImplementationGuideSpec spec, CancellationToken cancellationToken)
    {
        var authored = await _guideFactory.BuildAsync(spec, cancellationToken).ConfigureAwait(false);
        var verification = await _verifier
            .VerifyImplementationGuideAsync(authored.Guide, authored.Profiles, cancellationToken)
            .ConfigureAwait(false);

        if (verification.IsValid)
        {
            foreach (var profile in authored.Profiles)
                _registry.Register(profile);
            _registry.Register(authored.Guide);
        }

        return new AuthoredImplementationGuideResult(
            authored.Guide, authored.Profiles, verification, verification.IsValid);
    }
}
