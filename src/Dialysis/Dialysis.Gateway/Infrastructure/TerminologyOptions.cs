namespace Dialysis.Gateway.Infrastructure;

/// <summary>
/// Terminology service configuration. Phase 4.3.2.
/// </summary>
public sealed class TerminologyOptions
{
    public const string Section = "Terminology";
    public string? ServerUrl { get; set; }
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ServerUrl);
}
