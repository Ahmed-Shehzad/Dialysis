using System.Text.Json.Serialization;

namespace Dialysis.HIE.OpenEhr.Archetypes.Declarative;

/// <summary>
/// Declarative description of how a FHIR resource of one type is projected into an
/// archetype-shaped JSON payload. Loaded from a JSON document so a partner clinic can ship
/// new mappings as data — no recompile, no module deploy. The wire shape lives next to
/// this type in <c>Archetypes/Definitions/*.json</c>; the catalog loader at
/// <see cref="ArchetypeMappingCatalog"/> picks them up as embedded resources.
/// </summary>
public sealed record ArchetypeMappingDefinition
{
    /// <summary>
    /// Declarative description of how a FHIR resource of one type is projected into an
    /// archetype-shaped JSON payload. Loaded from a JSON document so a partner clinic can ship
    /// new mappings as data — no recompile, no module deploy. The wire shape lives next to
    /// this type in <c>Archetypes/Definitions/*.json</c>; the catalog loader at
    /// <see cref="ArchetypeMappingCatalog"/> picks them up as embedded resources.
    /// </summary>
    public ArchetypeMappingDefinition(string ArchetypeId,
        string FhirResourceType,
        string? Description,
        IReadOnlyList<FieldMapping> Fields)
    {
        this.ArchetypeId = ArchetypeId;
        this.FhirResourceType = FhirResourceType;
        this.Description = Description;
        this.Fields = Fields;
    }

    [JsonPropertyName("archetypeId")]
    public string ArchetypeId { get; init; }

    [JsonPropertyName("fhirResourceType")]
    public string FhirResourceType { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("fields")] public IReadOnlyList<FieldMapping> Fields { get; init; }
    public void Deconstruct(out string ArchetypeId, out string FhirResourceType, out string? Description, out IReadOnlyList<FieldMapping> Fields)
    {
        ArchetypeId = this.ArchetypeId;
        FhirResourceType = this.FhirResourceType;
        Description = this.Description;
        Fields = this.Fields;
    }
}

/// <summary>
/// One field in a mapping: a flat <see cref="Key"/> in the output JSON's <c>fields</c>
/// dictionary, populated from <see cref="Path"/> applied to the FHIR resource. Paths use
/// dotted FHIR property names with optional <c>[0]</c> indexing, <c>[?]</c> wildcards, and
/// <c>as TypeName</c> subtype casts. See <see cref="ResourcePath"/> for the grammar.
/// </summary>
public sealed record FieldMapping
{
    /// <summary>
    /// One field in a mapping: a flat <see cref="Key"/> in the output JSON's <c>fields</c>
    /// dictionary, populated from <see cref="Path"/> applied to the FHIR resource. Paths use
    /// dotted FHIR property names with optional <c>[0]</c> indexing, <c>[?]</c> wildcards, and
    /// <c>as TypeName</c> subtype casts. See <see cref="ResourcePath"/> for the grammar.
    /// </summary>
    public FieldMapping(string Key,
        string Path)
    {
        this.Key = Key;
        this.Path = Path;
    }
    [JsonPropertyName("key")] public string Key { get; init; }
    [JsonPropertyName("path")] public string Path { get; init; }
    public void Deconstruct(out string Key, out string Path)
    {
        Key = this.Key;
        Path = this.Path;
    }
}
