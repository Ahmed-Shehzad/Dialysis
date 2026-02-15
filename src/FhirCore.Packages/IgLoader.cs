using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace FhirCore.Packages;

public sealed class IgLoader : IIgLoader
{
    private readonly List<StructureDefinition> _definitions = [];
    private readonly PackageIndex _index = new();
    private readonly FhirJsonDeserializer _deserializer = new();

    public IReadOnlyList<StructureDefinition> LoadStructureDefinitions()
    {
        return _definitions;
    }

    public PackageIndex Index => _index;

    public void AddFromJson(string json)
    {
        var resource = _deserializer.Deserialize<StructureDefinition>(json);
        _definitions.Add(resource);
        _index.Index(resource);
    }

    public void LoadFromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(directoryPath, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                var json = File.ReadAllText(path);
                var resource = _deserializer.Deserialize<Resource>(json);
                if (resource is StructureDefinition sd)
                {
                    _definitions.Add(sd);
                    _index.Index(sd);
                }
            }
            catch
            {
                // Skip malformed or non-StructureDefinition files
            }
        }
    }
}
