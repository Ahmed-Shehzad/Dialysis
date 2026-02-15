namespace Dialysis.Documents.Configuration;

public sealed class DocumentsOptions
{
    public const string SectionName = "Documents";

    /// <summary>FHIR Gateway base URL for querying resources.</summary>
    public string FhirBaseUrl { get; set; } = "https://localhost:5000/fhir";

    /// <summary>Path to PDF template directory (AcroForm templates). When empty, fill-template is unavailable.</summary>
    public string? TemplatePath { get; set; }

    /// <summary>Comma-separated template IDs that support calculator pre-fill (e.g. adequacy, dialysis-adequacy). When includeScripts=true, Kt/V and URR are pre-calculated from input fields.</summary>
    public string? CalculatorTemplateIds { get; set; }

    /// <summary>Nutrient (GdPicture) license key. Empty string enables trial mode. Set for production.</summary>
    public string NutrientLicenseKey { get; set; } = "";
}
