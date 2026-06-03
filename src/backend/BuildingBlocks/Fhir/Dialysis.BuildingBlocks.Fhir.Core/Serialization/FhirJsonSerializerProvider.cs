using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace Dialysis.BuildingBlocks.Fhir.Serialization;

/// <summary>
/// Singleton holder for the Firely FHIR JSON serializer + deserializer. Constructing the
/// deserializer is expensive (reflection over the FHIR model); reuse one instance per host.
/// </summary>
public sealed class FhirJsonSerializerProvider
{
    // Recoverable mode mirrors the lenient behaviour of the pre-6.0 FhirJsonParser: it tolerates
    // recoverable issues (e.g. unmet cardinality) instead of throwing, leaving conformance checks
    // to the validation layer.
    private readonly FhirJsonDeserializer _deserializer =
        new(new DeserializerSettings().UsingMode(DeserializationMode.Recoverable));

    /// <summary>Serializes a FHIR resource to a JSON string.</summary>
    public string Serialize(Base resource, bool pretty = false) => resource.ToJson(pretty);

    /// <summary>Parses a FHIR JSON document into a resource of type <typeparamref name="T"/>.</summary>
    public T Parse<T>(string json) where T : Base => _deserializer.Deserialize<T>(json);

    /// <summary>Parses a FHIR JSON document into a resource.</summary>
    public Resource Parse(string json) => _deserializer.DeserializeResource(json);
}
