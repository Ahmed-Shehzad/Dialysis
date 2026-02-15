namespace Dialysis.Registry.Configuration;

public sealed class RegistryOptions
{
    public const string SectionName = "Registry";

    public string FhirBaseUrl { get; set; } = "https://localhost:5000/fhir";
}
