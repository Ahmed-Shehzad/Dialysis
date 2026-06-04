using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Fhir;

/// <summary>
/// Records which FHIR resource types modules have wired readers/searchers for, plus the
/// closure that performs runtime dispatch from a resource-type-name string. Consumed by the
/// default capability provider and the AspNetCore endpoint router.
/// </summary>
public sealed class FhirResourceRegistry
{
    private readonly Dictionary<string, FhirResourceCapability> _entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FhirReadDispatcher> _readDispatchers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FhirSearchDispatcher> _searchDispatchers = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, FhirResourceCapability> Entries => _entries;

    public bool TryGetReadDispatcher(string resourceType, out FhirReadDispatcher dispatcher)
        => _readDispatchers.TryGetValue(resourceType, out dispatcher!);

    public bool TryGetSearchDispatcher(string resourceType, out FhirSearchDispatcher dispatcher)
        => _searchDispatchers.TryGetValue(resourceType, out dispatcher!);

    public void RegisterReader<TResource>() where TResource : Resource, new()
    {
        var typeName = new TResource().TypeName;
        var capability = _entries.GetValueOrDefault(typeName) ?? new FhirResourceCapability(typeName);
        _entries[typeName] = capability with { SupportsRead = true };

        _readDispatchers[typeName] = async (sp, id, ct) =>
        {
            var reader = sp.GetRequiredService<IFhirReader<TResource>>();
            var result = await reader.ReadAsync(id, ct).ConfigureAwait(false);
            return new FhirReadDispatchResult(result.Resource, result.VersionId, result.LastModified);
        };
    }

    public void RegisterSearcher<TResource>() where TResource : Resource, new()
    {
        var typeName = new TResource().TypeName;
        var capability = _entries.GetValueOrDefault(typeName) ?? new FhirResourceCapability(typeName);
        _entries[typeName] = capability with { SupportsSearch = true };

        _searchDispatchers[typeName] = (sp, request, ct) =>
        {
            var searcher = sp.GetRequiredService<IFhirSearcher<TResource>>();
            return searcher.SearchAsync(request, ct);
        };
    }

    public void RegisterProfile<TResource>(string profileUrl) where TResource : Resource, new()
    {
        var typeName = new TResource().TypeName;
        var capability = _entries.GetValueOrDefault(typeName) ?? new FhirResourceCapability(typeName);
        var profiles = capability.SupportedProfiles.Append(profileUrl).Distinct(StringComparer.Ordinal).ToArray();
        _entries[typeName] = capability with { SupportedProfiles = profiles };
    }
}

public delegate ValueTask<FhirReadDispatchResult> FhirReadDispatcher(
    IServiceProvider serviceProvider,
    string id,
    CancellationToken cancellationToken);

public delegate ValueTask<Bundle> FhirSearchDispatcher(
    IServiceProvider serviceProvider,
    FhirSearchRequest request,
    CancellationToken cancellationToken);

public sealed record FhirReadDispatchResult
{
    public FhirReadDispatchResult(Resource? Resource, string? VersionId, DateTimeOffset? LastModified)
    {
        this.Resource = Resource;
        this.VersionId = VersionId;
        this.LastModified = LastModified;
    }
    public Resource? Resource { get; init; }
    public string? VersionId { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    public void Deconstruct(out Resource? Resource, out string? VersionId, out DateTimeOffset? LastModified)
    {
        Resource = this.Resource;
        VersionId = this.VersionId;
        LastModified = this.LastModified;
    }
}

public sealed record FhirResourceCapability
{
    public FhirResourceCapability(string TypeName,
        bool SupportsRead = false,
        bool SupportsSearch = false,
        IReadOnlyList<string>? SupportedProfiles = null)
    {
        this.TypeName = TypeName;
        this.SupportsRead = SupportsRead;
        this.SupportsSearch = SupportsSearch;
        this.SupportedProfiles = SupportedProfiles ?? Array.Empty<string>();
    }
    public IReadOnlyList<string> SupportedProfiles { get; init; }
    public string TypeName { get; init; }
    public bool SupportsRead { get; init; }
    public bool SupportsSearch { get; init; }
    public void Deconstruct(out string TypeName, out bool SupportsRead, out bool SupportsSearch, out IReadOnlyList<string>? SupportedProfiles)
    {
        TypeName = this.TypeName;
        SupportsRead = this.SupportsRead;
        SupportsSearch = this.SupportsSearch;
        SupportedProfiles = this.SupportedProfiles;
    }
}
