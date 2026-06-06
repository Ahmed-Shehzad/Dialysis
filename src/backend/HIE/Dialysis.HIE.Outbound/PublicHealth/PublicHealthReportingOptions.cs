namespace Dialysis.HIE.Outbound.PublicHealth;

/// <summary>
/// Configuration for electronic case reporting (<c>Hie:PublicHealth</c>). Reporting is a no-op until
/// an authority partner and a reportable-code list are configured — mirroring the retention-policy
/// posture (the DPO / public-health lead populates these per jurisdiction).
/// </summary>
public sealed class PublicHealthReportingOptions
{
    public const string SectionName = "Hie:PublicHealth";

    /// <summary>Partner id of the authorized public-health authority. Null = reporting disabled.</summary>
    public string? AuthorityPartnerId { get; set; }

    /// <summary>
    /// Codes (LOINC / SNOMED CT / ICD-10) that make a finding reportable. Empty = no-op. Matched
    /// case-insensitively by <see cref="ConfiguredReportabilityClassifier"/>.
    /// </summary>
    public List<string> ReportableCodes { get; init; } = [];

    /// <summary>True when both an authority partner and at least one reportable code are configured.</summary>
    public bool Enabled => !string.IsNullOrWhiteSpace(AuthorityPartnerId) && ReportableCodes.Count > 0;
}
