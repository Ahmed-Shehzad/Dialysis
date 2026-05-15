using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Fhir.BulkData;

/// <summary>
/// Binds a single resource type to a feeder lookup. Hosts register one instance per supported
/// resource type via <see cref="FhirBulkDataServiceCollectionExtensions.AddFhirBulkDataFeeder{TFeeder,TResource}"/>;
/// the orchestrator iterates the bound types when running an export.
/// </summary>
public interface INdjsonFeederBinding
{
    string ResourceType { get; }

    IAsyncEnumerable<Resource> StreamAsync(IServiceProvider services, ExportJob job, CancellationToken cancellationToken);
}

internal sealed class NdjsonFeederBinding<TResource> : INdjsonFeederBinding
    where TResource : Resource
{
    public string ResourceType { get; } = typeof(TResource).Name;

    public IAsyncEnumerable<Resource> StreamAsync(IServiceProvider services, ExportJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        var feeder = services.GetRequiredService<INdjsonResourceFeeder<TResource>>();
        return AsBaseAsync(feeder.StreamAsync(job, cancellationToken));
    }

    private static async IAsyncEnumerable<Resource> AsBaseAsync(IAsyncEnumerable<TResource> source)
    {
        await foreach (var resource in source.ConfigureAwait(false))
        {
            yield return resource;
        }
    }
}

/// <summary>
/// Resource-type → feeder dispatcher built from every <see cref="INdjsonFeederBinding"/> registered in DI.
/// </summary>
public sealed class NdjsonFeederBinder
{
    private readonly Dictionary<string, INdjsonFeederBinding> _bindings;

    public NdjsonFeederBinder(IEnumerable<INdjsonFeederBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        _bindings = bindings.ToDictionary(b => b.ResourceType, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<string> SupportedResourceTypes => _bindings.Keys;

    public bool TryGet(string resourceType, out INdjsonFeederBinding binding)
        => _bindings.TryGetValue(resourceType, out binding!);
}
