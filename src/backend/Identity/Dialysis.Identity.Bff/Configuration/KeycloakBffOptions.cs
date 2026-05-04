namespace Dialysis.Identity.Bff.Configuration;

public sealed class KeycloakBffOptions
{
    public const string SectionName = "Identity:Keycloak";

    /// <summary>Issuer base URL, e.g. <c>http://localhost:8080/realms/dialysis</c>.</summary>
    public string Authority { get; set; } = "";

    public string ClientId { get; set; } = "dialysis-bff";

    public string ClientSecret { get; set; } = "";

    /// <summary>Target client id for RFC 8693 token exchange (<c>audience</c> parameter).</summary>
    public string HisAudienceClientId { get; set; } = "dialysis-his-api";
}
