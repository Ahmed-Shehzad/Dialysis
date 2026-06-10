using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir;

/// <summary>
/// Per-resource type-level search facade. Returns a <c>Bundle</c> with <c>type=searchset</c>.
/// </summary>
// TResource is a phantom type parameter: it pins one searcher implementation per FHIR
// resource type for DI resolution even though the member signatures don't mention it.
#pragma warning disable S2326
public interface IFhirSearcher<TResource> where TResource : Resource
#pragma warning restore S2326
{
    ValueTask<Bundle> SearchAsync(FhirSearchRequest request, CancellationToken cancellationToken);
}
