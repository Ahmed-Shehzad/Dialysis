using System.Text.Json;
using Dialysis.HIE.Core.Abstraction.OpenEhr;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.OpenEhr.Archetypes.Declarative;

/// <summary>
/// Generic <see cref="IArchetypeProjection{TResource}"/> driven by a declarative
/// <see cref="ArchetypeMappingDefinition"/>. Walks every field's path against the FHIR
/// resource via <see cref="ResourcePath.Evaluate"/>, drops <c>null</c> results, and
/// serialises the (key → value) dictionary together with the archetype id.
///
/// Output shape — one JSON document per resource:
/// <code>
/// {
///   "archetype_node_id": "openEHR-EHR-OBSERVATION.lab_test_result.v1",
///   "fields": {
///     "code.code": "29463-7",
///     "code.system": "http://loinc.org",
///     "value.magnitude": 75.5,
///     "value.unit": "kg",
///     "effective_time": "2026-06-03T12:00:00Z"
///   }
/// }
/// </code>
/// The flat <c>fields</c> dictionary keyed by archetype-style paths matches the openEHR
/// CDR ingestion convention so a future EHRbase / Better Platform sink can consume the
/// payload without further translation.
/// </summary>
public sealed class DeclarativeArchetypeProjection<TResource> : IArchetypeProjection<TResource>
    where TResource : Resource
{
    private readonly ArchetypeMappingDefinition _definition;

    public DeclarativeArchetypeProjection(ArchetypeMappingDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definition = definition;
    }

    public string ArchetypeId => _definition.ArchetypeId;

    public string Project(TResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        var fields = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var mapping in _definition.Fields)
        {
            var value = ResourcePath.Evaluate(resource, mapping.Path);
            if (value is null) continue;
            fields[mapping.Key] = value;
        }
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["archetype_node_id"] = _definition.ArchetypeId,
            ["fields"] = fields,
        };
        return JsonSerializer.Serialize(payload);
    }
}
