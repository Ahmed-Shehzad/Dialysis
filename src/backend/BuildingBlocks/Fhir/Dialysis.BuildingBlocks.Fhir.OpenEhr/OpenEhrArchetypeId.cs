namespace Dialysis.BuildingBlocks.Fhir.OpenEhr;

/// <summary>
/// openEHR archetype identifier, e.g. <c>openEHR-EHR-OBSERVATION.blood_pressure.v2</c>.
/// </summary>
public sealed record OpenEhrArchetypeId
{
    /// <summary>
    /// openEHR archetype identifier, e.g. <c>openEHR-EHR-OBSERVATION.blood_pressure.v2</c>.
    /// </summary>
    public OpenEhrArchetypeId(string ns, string ConceptName, int MajorVersion)
    {
        Namespace = ns;
        this.ConceptName = ConceptName;
        this.MajorVersion = MajorVersion;
    }
    public string Value => $"{Namespace}.{ConceptName}.v{MajorVersion}";
    public string Namespace { get; init; }
    public string ConceptName { get; init; }
    public int MajorVersion { get; init; }

    public static OpenEhrArchetypeId Parse(string raw)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raw);
        var parts = raw.Split('.');
        if (parts.Length < 3)
            throw new FormatException($"Invalid archetype id: {raw}");
        var versionToken = parts[^1].TrimStart('v', 'V');
        if (!int.TryParse(versionToken, out var major))
            throw new FormatException($"Archetype id missing version: {raw}");
        return new OpenEhrArchetypeId(parts[0], string.Join('.', parts[1..^1]), major);
    }
    public void Deconstruct(out string @namespace, out string conceptName, out int majorVersion)
    {
        @namespace = this.Namespace;
        conceptName = this.ConceptName;
        majorVersion = this.MajorVersion;
    }
}
