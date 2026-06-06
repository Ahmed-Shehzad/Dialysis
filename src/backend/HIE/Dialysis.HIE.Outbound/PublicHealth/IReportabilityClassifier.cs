using Microsoft.Extensions.Options;

namespace Dialysis.HIE.Outbound.PublicHealth;

/// <summary>
/// Decides whether a clinical code makes a finding reportable to public health. Deterministic
/// code-list match (no external RCKMS dependency); a richer rules engine slots in behind this seam.
/// </summary>
public interface IReportabilityClassifier
{
    bool IsReportable(string? code);
}

public sealed class ConfiguredReportabilityClassifier : IReportabilityClassifier
{
    private readonly HashSet<string> _codes;
    public ConfiguredReportabilityClassifier(IOptions<PublicHealthReportingOptions> options) =>
        _codes = new HashSet<string>(options.Value.ReportableCodes, StringComparer.OrdinalIgnoreCase);

    public bool IsReportable(string? code) => !string.IsNullOrWhiteSpace(code) && _codes.Contains(code);
}
