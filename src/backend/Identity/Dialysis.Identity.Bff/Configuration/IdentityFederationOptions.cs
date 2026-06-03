namespace Dialysis.Identity.Bff.Configuration;

/// <summary>
/// Configuration for upstream identity-provider brokering. Keycloak stays the only direct OIDC
/// client of the BFF; each entry here describes an additional IdP (Okta, Auth0, Entra, …) that
/// is brokered through Keycloak via its <c>kc_idp_hint</c> parameter. The BFF surfaces this
/// catalog as <c>GET /identity/providers</c> for the SPA login page; an entry's <c>Alias</c>
/// must match the Keycloak realm <c>identityProviders[].alias</c> verbatim.
/// </summary>
public sealed class IdentityFederationOptions
{
    public const string SectionName = "Identity:Federation";

    /// <summary>Upstream IdPs brokered through Keycloak. May be empty (single-IdP deployments).</summary>
    public IList<IdentityProviderEntry> Providers { get; set; } = [];
}

/// <summary>
/// A single brokered upstream IdP. The <see cref="Alias"/> is the value passed as
/// <c>kc_idp_hint</c> on the OIDC auth request; Keycloak uses it to skip its own login page
/// and immediately redirect to the upstream IdP.
/// </summary>
public sealed class IdentityProviderEntry
{
    /// <summary>Keycloak realm broker alias (e.g. <c>okta</c>, <c>auth0</c>, <c>entra</c>).</summary>
    public string Alias { get; set; } = "";

    /// <summary>Human-readable label shown on the SPA login page.</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>Optional icon URL the SPA renders next to the button. Path or absolute URL.</summary>
    public string? IconUri { get; set; }

    /// <summary>When <c>false</c>, the entry is hidden from the SPA catalog. Default <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;
}
