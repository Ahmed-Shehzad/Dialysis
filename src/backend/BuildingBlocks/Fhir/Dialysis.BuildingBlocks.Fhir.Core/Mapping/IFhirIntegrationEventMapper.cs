using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Mapping;

/// <summary>
/// Marker specialization of <see cref="IFhirResourceMapper{TSource, TResource}"/> for integration-event sources.
/// Same shape; separate type enables clean DI registration of event-driven mapping pipelines.
/// </summary>
public interface IFhirIntegrationEventMapper<in TEvent, out TResource> : IFhirResourceMapper<TEvent, TResource>
    where TEvent : class
    where TResource : Resource
{
}
