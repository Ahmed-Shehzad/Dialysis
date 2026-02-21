namespace BuildingBlocks.ExceptionHandling;

/// <summary>
/// Configuration for exception report email delivery.
/// C5: Store credentials in configuration or Key Vault; never hardcode.
/// </summary>
public sealed class ExceptionReportEmailOptions
{
    public const string SectionName = "ExceptionHandling:Email";

    public bool Enabled { get; set; }
    public string DevelopmentEmail { get; set; } = string.Empty;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public string FromAddress { get; set; } = "noreply@example.com";
    public string FromDisplayName { get; set; } = "Dialysis PDMS Error Reporter";
}
