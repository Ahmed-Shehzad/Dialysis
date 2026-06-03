using System.Text.Json.Serialization;

namespace Dialysis.HIE.OpenEhr.Archetypes.Declarative;

/// <summary>
/// Declarative description of how a FHIR resource of one type is projected into an
/// archetype-shaped JSON payload. Loaded from a JSON document so a partner clinic can ship
/// new mappings as data — no recompile, no module deploy. The wire shape lives next to
/// this type in <c>Archetypes/Definitions/*.json</c>; the catalog loader at
/// <see cref="ArchetypeMappingCatalog"/> picks them up as embedded resources.
/// </summary>
public sealed record ArchetypeMappingDefinition(
    [property: JsonPropertyName("archetypeId")] string ArchetypeId,
    [property: JsonPropertyName("fhirResourceType")] string FhirResourceType,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("fields")] IReadOnlyList<FieldMapping> Fields);

/// <summary>
/// One field in a mapping: a flat <see cref="Key"/> in the output JSON's <c>fields</c>
/// dictionary, populated from <see cref="Path"/> applied to the FHIR resource. Paths use
/// dotted FHIR property names with optional <c>[0]</c> indexing, <c>[?]</c> wildcards, and
/// <c>as TypeName</c> subtype casts. See <see cref="ResourcePath"/> for the grammar.
/// </summary>
public sealed record FieldMapping(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("path")] string Path);
