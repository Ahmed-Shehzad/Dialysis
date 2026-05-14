using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Mapping;

/// <summary>
/// Cross-cutting contract for translating a domain type or integration event into a FHIR R4 resource.
/// Replaces the legacy <c>Dialysis.HIE.Core.Abstraction.Mapping.IFhirMapper&lt;,&gt;</c> with a
/// building-block-owned interface so any module can implement and register mappers.
/// </summary>
public interface IFhirResourceMapper<in TSource, out TResource>
    where TResource : Resource
{
    TResource Map(TSource source);
}
