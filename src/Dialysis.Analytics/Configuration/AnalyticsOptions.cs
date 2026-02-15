namespace Dialysis.Analytics.Configuration;

public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    /// <summary>FHIR Gateway base URL (e.g. https://gateway-host/fhir).</summary>
    public string FhirBaseUrl { get; set; } = "https://localhost:5000/fhir";

    /// <summary>Alerting API base URL (e.g. https://alerting-host).</summary>
    public string AlertingBaseUrl { get; set; } = "https://localhost:5003";

    /// <summary>AuditConsent API base URL for AuditEvent emission (e.g. https://audit-consent-host). When empty, audit is no-op.</summary>
    public string? AuditConsentBaseUrl { get; set; }

    /// <summary>PostgreSQL connection string for saved cohorts. When empty, uses InMemorySavedCohortStore.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>PublicHealth service base URL for research export de-identification (e.g. https://public-health-host). When empty, research export skips de-id.</summary>
    public string? PublicHealthBaseUrl { get; set; }
}
