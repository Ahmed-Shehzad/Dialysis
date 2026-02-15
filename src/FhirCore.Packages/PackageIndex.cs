using Hl7.Fhir.Model;

namespace FhirCore.Packages;

public sealed class PackageIndex
{
    private readonly Dictionary<string, StructureDefinition> _byUrl = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, StructureDefinition> _byCanonical = new(StringComparer.OrdinalIgnoreCase);

    public void Index(StructureDefinition definition)
    {
        if (!string.IsNullOrEmpty(definition.Url))
        {
            _byUrl[definition.Url] = definition;
        }

        if (!string.IsNullOrEmpty(definition.Url) && !string.IsNullOrEmpty(definition.Version))
        {
            var canonical = $"{definition.Url}|{definition.Version}";
            _byCanonical[canonical] = definition;
        }
    }

    public StructureDefinition? ResolveByUrl(string url)
    {
        return _byUrl.TryGetValue(url, out var def) ? def : null;
    }

    public StructureDefinition? ResolveByCanonical(string canonical)
    {
        return _byCanonical.TryGetValue(canonical, out var def) ? def : null;
    }
}
