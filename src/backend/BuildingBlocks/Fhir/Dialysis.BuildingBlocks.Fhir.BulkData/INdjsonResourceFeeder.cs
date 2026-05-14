using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.BulkData;

/// <summary>
/// Per-resource-type feeder a module supplies to stream resources for inclusion in a bulk export.
/// Implementations should keep memory bounded by yielding resources incrementally.
/// </summary>
public interface INdjsonResourceFeeder<TResource> where TResource : Resource
{
    IAsyncEnumerable<TResource> StreamAsync(ExportJob job, CancellationToken cancellationToken);
}
