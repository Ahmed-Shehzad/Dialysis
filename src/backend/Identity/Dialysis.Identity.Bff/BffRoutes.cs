namespace Dialysis.Identity.Bff;

// Centralising the route base keeps the OIDC callback paths, minimal-API routes, and YARP
// forwarding rules in sync. Drift between them is what produces "Invalid parameter: redirect_uri"
// from Keycloak: the OIDC handler builds redirect_uri from CallbackPath + request host, so
// CallbackPath must match the path the request actually arrives on (gateway preserves /identity).
internal static class BffRoutes
{
    public const string Base = "/identity";
    public const string Root = Base + "/";
    public const string Login = Base + "/login";
    public const string Logout = Base + "/logout";
    public const string User = Base + "/user";
    public const string SignInCallback = Base + "/signin-oidc";
    public const string SignedOutCallback = Base + "/signout-callback-oidc";
}
