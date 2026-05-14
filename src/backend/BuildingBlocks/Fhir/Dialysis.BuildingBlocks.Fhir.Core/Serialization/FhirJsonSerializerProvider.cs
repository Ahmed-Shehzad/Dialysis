using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace Dialysis.BuildingBlocks.Fhir.Serialization;

/// <summary>
/// Singleton holder for the Firely FHIR JSON serializer + parser instances. Initialising these
/// is expensive (reflection over the FHIR model); reuse one pair per host.
/// </summary>
public sealed class FhirJsonSerializerProvider
{
    public FhirJsonSerializerProvider()
    {
        Serializer = new FhirJsonSerializer(new SerializerSettings { Pretty = false });
        PrettySerializer = new FhirJsonSerializer(new SerializerSettings { Pretty = true });
        Parser = new FhirJsonParser(new ParserSettings { AcceptUnknownMembers = false });
    }

    public FhirJsonSerializer Serializer { get; }

    public FhirJsonSerializer PrettySerializer { get; }

    public FhirJsonParser Parser { get; }

    public string Serialize(Base resource, bool pretty = false) =>
        pretty ? PrettySerializer.SerializeToString(resource) : Serializer.SerializeToString(resource);
}
