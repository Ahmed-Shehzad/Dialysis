using Hl7.Fhir.Model;

namespace Dialysis.HIE.Core.Abstraction.OpenEhr;

/// <summary>
/// Projects a FHIR resource into an openEHR archetype-shaped JSON payload for durable storage.
/// Lets the HIE retain a longitudinal record using openEHR semantics while keeping FHIR on the wire.
/// </summary>
public interface IArchetypeProjection<in TResource> where TResource : Resource
{
    string ArchetypeId { get; }

    string Project(TResource resource);
}
