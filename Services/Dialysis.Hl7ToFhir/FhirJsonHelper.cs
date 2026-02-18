using System.Text.Json;

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Helper for serializing FHIR R4 resources to JSON (application/fhir+json).
/// </summary>
public static class FhirJsonHelper
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string ToJson(Resource resource) => JsonSerializer.Serialize(resource, resource.GetType(), Options);

    private static JsonSerializerOptions CreateOptions()
    {
#pragma warning disable CS0618 // ForFhir(Assembly) obsolete - use ModelInspector when available
        JsonSerializerOptions options = new JsonSerializerOptions().ForFhir(typeof(ModelInfo).Assembly);
#pragma warning restore CS0618
        options.WriteIndented = false;
        return options;
    }
}
