using Hl7.Fhir.Model;

namespace FhirCore.Packages;

public interface IIgLoader
{
    IReadOnlyList<StructureDefinition> LoadStructureDefinitions();
}
