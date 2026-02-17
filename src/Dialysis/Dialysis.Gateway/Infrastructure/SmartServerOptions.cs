namespace Dialysis.Gateway.Infrastructure;

/// <summary>
/// SMART on FHIR server configuration. PDMS as FHIR server for EHR-launched apps.
/// </summary>
public sealed class SmartServerOptions
{
    public const string Section = "Smart";

    /// <summary>PDMS base URL (e.g. https://pdms.example.com). Used for discovery and auth endpoints.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>HMAC signing key for access tokens (base64). Generate with: openssl rand -base64 32</summary>
    public string? SigningKey { get; set; }

    /// <summary>Authorized client for standalone/launch. If empty, any client is accepted (dev only).</summary>
    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(SigningKey);
}
