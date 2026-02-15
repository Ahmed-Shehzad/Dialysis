namespace Dialysis.PublicHealth.Configuration;

public sealed class PublicHealthOptions
{
    public const string SectionName = "PublicHealth";

    /// <summary>FHIR Gateway base URL for querying resources.</summary>
    public string FhirBaseUrl { get; set; } = "https://localhost:5000/fhir";

    /// <summary>Path to reportable conditions JSON (e.g. docs/reportable-conditions/REPORTABLE-CONDITIONS-REGISTRY.json). When set and file exists, loads from config.</summary>
    public string? ReportableConditionsConfigPath { get; set; }

    /// <summary>Public health endpoint URL for report delivery (push). When empty, delivery is no-op.</summary>
    public string? ReportDeliveryEndpoint { get; set; }

    /// <summary>Report delivery format: fhir (JSON) or hl7v2. Default: fhir.</summary>
    public string ReportDeliveryFormat { get; set; } = "fhir";

    /// <summary>HL7 v2 MSH-3 (sending application) for report delivery.</summary>
    public string? Hl7SendingApp { get; set; }

    /// <summary>HL7 v2 MSH-4 (sending facility) for report delivery.</summary>
    public string? Hl7SendingFacility { get; set; }
}
