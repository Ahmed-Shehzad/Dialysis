using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.OpenEhr;

/// <summary>Maps an openEHR composition/entry payload into a FHIR R4 resource.</summary>
public interface IOpenEhrToFhirMapper<in TArchetypePayload, out TFhirResource>
    where TFhirResource : Resource
{
    OpenEhrArchetypeId ArchetypeId { get; }

    TFhirResource Map(TArchetypePayload payload);
}

/// <summary>Maps a FHIR R4 resource into an openEHR composition/entry payload.</summary>
public interface IFhirToOpenEhrMapper<in TFhirResource, out TArchetypePayload>
    where TFhirResource : Resource
{
    OpenEhrArchetypeId ArchetypeId { get; }

    TArchetypePayload Map(TFhirResource resource);
}
