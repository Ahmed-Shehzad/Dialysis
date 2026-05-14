using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir;

/// <summary>
/// Per-resource read facade. One implementation per (module, resource type).
/// </summary>
public interface IFhirReader<TResource> where TResource : Resource
{
    ValueTask<FhirReadResult<TResource>> ReadAsync(string id, CancellationToken cancellationToken);
}
