namespace Dialysis.Module.Bff;

/// <summary>
/// The auth route paths for one BFF, all computed from its base path (e.g. <c>/his</c>). Keeping
/// the OIDC callback paths and the minimal-API routes derived from one base keeps them in sync —
/// drift is what produces "Invalid parameter: redirect_uri" from Keycloak, because the OIDC
/// handler builds <c>redirect_uri</c> from <c>CallbackPath</c> + host and it must match the path
/// the request actually arrives on (the gateway preserves the full <c>{base}/identity/...</c> path).
/// </summary>
public sealed class BffRoutePaths
{
    public BffRoutePaths(string basePath)
    {
        var b = "/" + (basePath ?? "").Trim('/');
        Base = b + "/identity";
        Root = Base;
        Login = Base + "/login";
        Logout = Base + "/logout";
        User = Base + "/user";
        Providers = Base + "/providers";
        SignInCallback = Base + "/signin-oidc";
        SignedOutCallback = Base + "/signout-callback-oidc";
    }

    /// <summary>The <c>{base}/identity</c> prefix every auth route lives under.</summary>
    public string Base { get; }
    public string Root { get; }
    public string Login { get; }
    public string Logout { get; }
    public string User { get; }
    public string Providers { get; }
    public string SignInCallback { get; }
    public string SignedOutCallback { get; }
}
