using System.Collections.Concurrent;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;

namespace Dialysis.BuildingBlocks.Fhir.Validation.Authoring;

/// <summary>
/// Holds profiles / implementation guides authored at runtime and makes them resolvable on the fly.
/// Implements <see cref="IAsyncResourceResolver"/> so the snapshot generator and downstream
/// validators can resolve authored canonicals (enabling profile-on-profile layering) while falling
/// back to the bundled FHIR R4 core specification for base definitions.
/// </summary>
public interface IFhirConformanceRegistry : IAsyncResourceResolver
{
    void Register(Resource resource);

    /// <summary>
    /// Registers any conformance resource under an explicit canonical URL — used when loading
    /// external FHIR packages (US Core, CH Core, …) whose resources span many resource types.
    /// </summary>
    void Register(string canonicalUrl, Resource resource);

    bool TryGet(string canonicalUrl, out Resource? resource);

    IReadOnlyCollection<StructureDefinition> Profiles { get; }

    IReadOnlyCollection<ImplementationGuide> ImplementationGuides { get; }
}

/// <inheritdoc cref="IFhirConformanceRegistry" />
public sealed class AuthoredConformanceRegistry : IFhirConformanceRegistry
{
    private readonly ConcurrentDictionary<string, Resource> _byCanonical = new(StringComparer.Ordinal);
    private readonly Lazy<IAsyncResourceResolver> _core;

    public AuthoredConformanceRegistry()
        : this(CoreSpecificationSource.Create)
    {
    }

    /// <summary>Test seam: supply the core (base-spec) resolver factory explicitly.</summary>
    public AuthoredConformanceRegistry(Func<IAsyncResourceResolver> coreResolverFactory)
    {
        ArgumentNullException.ThrowIfNull(coreResolverFactory);
        _core = new Lazy<IAsyncResourceResolver>(
            coreResolverFactory, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IReadOnlyCollection<StructureDefinition> Profiles
        => [.. _byCanonical.Values.OfType<StructureDefinition>()];

    public IReadOnlyCollection<ImplementationGuide> ImplementationGuides
        => [.. _byCanonical.Values.OfType<ImplementationGuide>()];

    public void Register(Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        var canonical = resource switch
        {
            StructureDefinition sd => sd.Url,
            ImplementationGuide ig => ig.Url,
            ValueSet vs => vs.Url,
            CodeSystem cs => cs.Url,
            _ => null,
        };
        if (string.IsNullOrWhiteSpace(canonical))
            throw new ArgumentException(
                $"Resource of type {resource.TypeName} has no canonical URL to register under.",
                nameof(resource));

        Register(canonical, resource);
    }

    public void Register(string canonicalUrl, Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        if (string.IsNullOrWhiteSpace(canonicalUrl))
            throw new ArgumentException("Canonical URL is required.", nameof(canonicalUrl));

        _byCanonical[StripVersion(canonicalUrl)] = resource;
    }

    public bool TryGet(string canonicalUrl, out Resource? resource)
    {
        if (_byCanonical.TryGetValue(StripVersion(canonicalUrl), out var found))
        {
            resource = found;
            return true;
        }

        resource = null;
        return false;
    }

    public async Task<Resource?> ResolveByCanonicalUriAsync(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return null;
        if (_byCanonical.TryGetValue(StripVersion(uri), out var authored))
            return authored;
        return await _core.Value.ResolveByCanonicalUriAsync(uri).ConfigureAwait(false);
    }

    public async Task<Resource?> ResolveByUriAsync(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return null;
        if (_byCanonical.TryGetValue(StripVersion(uri), out var authored))
            return authored;
        return await _core.Value.ResolveByUriAsync(uri).ConfigureAwait(false);
    }

    // Canonicals may carry a |version suffix; authored artifacts are indexed without it.
    private static string StripVersion(string uri)
    {
        var bar = uri.IndexOf('|', StringComparison.Ordinal);
        return bar < 0 ? uri : uri[..bar];
    }
}
