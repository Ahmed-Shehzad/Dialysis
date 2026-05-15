namespace Dialysis.Identity.Bff.Configuration;

/// <summary>
/// SPA-facing options. The post-login redirect must be on the allowlist to prevent
/// open-redirect attacks against the OIDC callback.
/// </summary>
public sealed class BffSpaOptions
{
    public const string SectionName = "Identity:Spa";

    /// <summary>Default URL the BFF redirects to after sign-in if the caller doesn't supply one.</summary>
    public string DefaultReturnUrl { get; set; } = "/";

    /// <summary>Allowed absolute URL prefixes for <c>?returnUrl=</c>; protects against open redirects.</summary>
    public IList<string> AllowedReturnUrlPrefixes { get; set; } = [];
}
