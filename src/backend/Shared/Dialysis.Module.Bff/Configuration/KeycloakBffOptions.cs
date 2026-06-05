namespace Dialysis.Module.Bff.Configuration;

/// <summary>OIDC client settings the BFF uses to talk to Keycloak (or any OIDC IdP).</summary>
public sealed class KeycloakBffOptions
{
    public const string SectionName = "Bff:Keycloak";

    /// <summary>Issuer base URL, e.g. <c>http://localhost:8080/realms/dialysis</c>.</summary>
    public string Authority { get; set; } = "";

    /// <summary>Per-context confidential client id, e.g. <c>dialysis-his-bff</c>.</summary>
    public string ClientId { get; set; } = "";

    public string ClientSecret { get; set; } = "";
}
