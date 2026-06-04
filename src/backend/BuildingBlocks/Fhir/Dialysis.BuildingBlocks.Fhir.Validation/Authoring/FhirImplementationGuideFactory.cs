using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Validation.Authoring;

/// <summary>The artifacts produced when authoring an Implementation Guide on demand.</summary>
public sealed record AuthoredImplementationGuide
{
    /// <summary>The artifacts produced when authoring an Implementation Guide on demand.</summary>
    public AuthoredImplementationGuide(ImplementationGuide Guide,
        IReadOnlyList<StructureDefinition> Profiles)
    {
        this.Guide = Guide;
        this.Profiles = Profiles;
    }
    public ImplementationGuide Guide { get; init; }
    public IReadOnlyList<StructureDefinition> Profiles { get; init; }
    public void Deconstruct(out ImplementationGuide Guide, out IReadOnlyList<StructureDefinition> Profiles)
    {
        Guide = this.Guide;
        Profiles = this.Profiles;
    }
}

/// <summary>Builds an <c>ImplementationGuide</c> plus its contained profiles from a spec.</summary>
public interface IFhirImplementationGuideFactory
{
    Task<AuthoredImplementationGuide> BuildAsync(
        FhirImplementationGuideSpec spec, CancellationToken cancellationToken);
}

/// <inheritdoc cref="IFhirImplementationGuideFactory" />
public sealed class FhirImplementationGuideFactory : IFhirImplementationGuideFactory
{
    private readonly IFhirProfileFactory _profileFactory;
    /// <inheritdoc cref="IFhirImplementationGuideFactory" />
    public FhirImplementationGuideFactory(IFhirProfileFactory profileFactory) => _profileFactory = profileFactory;
    public async Task<AuthoredImplementationGuide> BuildAsync(
        FhirImplementationGuideSpec spec, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (string.IsNullOrWhiteSpace(spec.PackageId))
            throw new ArgumentException("FhirImplementationGuideSpec.PackageId is required.", nameof(spec));
        if (string.IsNullOrWhiteSpace(spec.Url))
            throw new ArgumentException("FhirImplementationGuideSpec.Url is required.", nameof(spec));
        if (string.IsNullOrWhiteSpace(spec.Name))
            throw new ArgumentException("FhirImplementationGuideSpec.Name is required.", nameof(spec));

        var profiles = new List<StructureDefinition>(spec.Profiles.Count);
        foreach (var profileSpec in spec.Profiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            profiles.Add(await _profileFactory.BuildAsync(profileSpec, cancellationToken).ConfigureAwait(false));
        }

        var ig = new ImplementationGuide
        {
            Url = spec.Url,
            Id = spec.Name,
            Name = spec.Name,
            Title = spec.Title ?? spec.Name,
            Status = PublicationStatus.Active,
            Experimental = false,
            PackageId = spec.PackageId,
            Version = spec.Version,
            FhirVersion = [FHIRVersion.N4_0_1],
            Date = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
            DependsOn = [.. spec.DependsOn.Select(d => new ImplementationGuide.DependsOnComponent
            {
                Uri = d.Uri,
                PackageId = d.PackageId,
                Version = d.Version,
            })],
            Global = [.. profiles.Select(p => new ImplementationGuide.GlobalComponent
            {
                Type = ToResourceType(p.Type),
                Profile = p.Url,
            })],
            Definition = new ImplementationGuide.DefinitionComponent
            {
                Resource = [.. profiles.Select(p => new ImplementationGuide.ResourceComponent
                {
                    Reference = new ResourceReference($"StructureDefinition/{p.Id}"),
                    Name = p.Title ?? p.Name,
                    Description = p.Description,
                    Example = new FhirBoolean(false),
                })],
            },
        };

        return new AuthoredImplementationGuide(ig, profiles);
    }

    private static ResourceType? ToResourceType(string? typeName)
        => Enum.TryParse<ResourceType>(typeName, ignoreCase: true, out var rt) ? rt : null;
}
