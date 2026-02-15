namespace Dialysis.Auth;

/// <summary>
/// OIDC/JWT authentication configuration for production.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// OIDC issuer URL (e.g. Azure AD, Keycloak, Auth0).
    /// Production: Required. Development: defaults to https://localhost:5001.
    /// </summary>
    public string Authority { get; set; } = "https://localhost:5001";

    /// <summary>
    /// Expected JWT audience (API identifier).
    /// Production: Required. Development: defaults to dialysis-api.
    /// </summary>
    public string Audience { get; set; } = "dialysis-api";

    /// <summary>
    /// Require HTTPS for metadata discovery (recommended in production).
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    public bool IsProductionConfigured =>
        !string.IsNullOrWhiteSpace(Authority) &&
        !string.IsNullOrWhiteSpace(Audience) &&
        Authority != "https://localhost:5001";
}
