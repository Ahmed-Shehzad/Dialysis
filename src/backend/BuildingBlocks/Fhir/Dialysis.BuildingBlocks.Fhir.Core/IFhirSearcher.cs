using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir;

/// <summary>
/// Per-resource type-level search facade. Returns a <c>Bundle</c> with <c>type=searchset</c>.
/// </summary>
public interface IFhirSearcher<TResource> where TResource : Resource
{
    ValueTask<Bundle> SearchAsync(FhirSearchRequest request, CancellationToken cancellationToken);
}
