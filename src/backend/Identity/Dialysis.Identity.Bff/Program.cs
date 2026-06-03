using System.Net.Http.Headers;
using Dialysis.Identity.Bff.Configuration;
using Dialysis.Identity.Bff.Federation;
using Dialysis.Identity.Bff.Services;
using Dialysis.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Dialysis.Identity.Bff;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddAuthorization();
builder.Services.Configure<KeycloakBffOptions>(builder.Configuration.GetSection(KeycloakBffOptions.SectionName));
builder.Services.Configure<BffSpaOptions>(builder.Configuration.GetSection(BffSpaOptions.SectionName));
builder.Services.Configure<IdentityFederationOptions>(builder.Configuration.GetSection(IdentityFederationOptions.SectionName));
builder.Services.AddSingleton<IIdentityProviderCatalog, ConfiguredIdentityProviderCatalog>();
builder.Services.AddHttpClient("keycloak");
builder.Services.AddScoped<IHisAccessTokenProvider, HisAccessTokenProvider>();

var kc = builder.Configuration.GetSection(KeycloakBffOptions.SectionName).Get<KeycloakBffOptions>()
         ?? new KeycloakBffOptions();
var authority = kc.Authority?.Trim() ?? "";
if (string.IsNullOrEmpty(authority))
    throw new InvalidOperationException(
        $"Set {KeycloakBffOptions.SectionName}:Authority to your Keycloak realm issuer (e.g. http://localhost:8080/realms/dialysis).");

var requireHttpsMetadata = Uri.TryCreate(authority, UriKind.Absolute, out var issuerUri)
    && string.Equals(issuerUri.Scheme, "https", StringComparison.OrdinalIgnoreCase);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(o =>
    {
        o.Cookie.Name = "Dialysis.Identity.Bff";
        o.Cookie.SameSite = SameSiteMode.Lax;
    })
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, o =>
    {
        o.Authority = authority.TrimEnd('/');
        o.ClientId = kc.ClientId;
        o.ClientSecret = kc.ClientSecret;
        o.ResponseType = OpenIdConnectResponseType.Code;
        o.SaveTokens = true;
        o.GetClaimsFromUserInfoEndpoint = true;
        o.MapInboundClaims = false;
        o.TokenValidationParameters.NameClaimType = "preferred_username";
        o.TokenValidationParameters.RoleClaimType = "roles";
        o.RequireHttpsMetadata = requireHttpsMetadata;
        // All BFF routes (login/logout/callback) live under the /identity prefix so they
        // round-trip cleanly through the edge gateway (which routes /identity/{**catch-all}
        // to this BFF without stripping the prefix). Direct-to-BFF dev hits the same paths.
        o.CallbackPath = BffRoutes.SignInCallback;
        o.SignedOutCallbackPath = BffRoutes.SignedOutCallback;
        // The OIDC correlation + nonce cookies default to SameSite=None, which modern
        // browsers reject without Secure — and Secure requires HTTPS. On HTTP localhost dev
        // that means the correlation cookie is silently dropped, and when Keycloak redirects
        // back to /identity/signin-oidc the handler can't find the nonce and aborts the
        // login → user lands back on /login with no visible error. SameSite=Lax survives
        // the cross-site redirect through Keycloak (browsers send Lax cookies on top-level
        // GET navigations) and does not need Secure, so the flow completes cleanly on HTTP.
        o.CorrelationCookie.SameSite = SameSiteMode.Lax;
        o.NonceCookie.SameSite = SameSiteMode.Lax;
        o.Scope.Add("openid");
        o.Scope.Add("profile");
        o.Scope.Add("email");

        // Multi-IdP federation through Keycloak brokering. When the /identity/login handler
        // stashes a known broker alias in AuthenticationProperties.Items["kc_idp_hint"],
        // forward it as a parameter on the OIDC auth-request URL. Keycloak sees the hint and
        // skips its own login page, immediately redirecting to the upstream IdP (Okta/Auth0/
        // Entra). Unknown aliases are filtered upstream by IIdentityProviderCatalog.IsKnown,
        // so we trust the value here.
        o.Events.OnRedirectToIdentityProvider = ctx =>
        {
            if (ctx.Properties.Items.TryGetValue("kc_idp_hint", out var hint)
                && !string.IsNullOrWhiteSpace(hint))
            {
                ctx.ProtocolMessage.SetParameter("kc_idp_hint", hint);
            }
            return Task.CompletedTask;
        };
    });

var proxySection = builder.Configuration.GetSection("ReverseProxy");
var hisClusterAddress = builder.Configuration["ReverseProxy:Clusters:his:Destinations:d1:Address"];
var enableHisProxy = !string.IsNullOrWhiteSpace(hisClusterAddress)
    && Uri.TryCreate(hisClusterAddress, UriKind.Absolute, out _);

if (enableHisProxy)
{
    builder.Services.AddReverseProxy()
        .LoadFromConfig(proxySection)
        .AddTransforms(transforms =>
        {
            transforms.AddRequestTransform(async context =>
            {
                var provider = context.HttpContext.RequestServices.GetRequiredService<IHisAccessTokenProvider>();
                var token = await provider.GetAccessTokenForHisAsync(context.HttpContext.RequestAborted)
                    .ConfigureAwait(false);
                if (!string.IsNullOrEmpty(token))
                    context.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            });
        });
}

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet(BffRoutes.Root, () => Results.Text(
    $"Dialysis Identity BFF — GET {BffRoutes.Login} to sign in, GET {BffRoutes.User} for claims, "
    + $"{BffRoutes.Base}/his/... to proxy to HIS when ReverseProxy is configured.",
    "text/plain"));

static string ResolveReturnUrl(string? requested, BffSpaOptions spa)
{
    if (string.IsNullOrWhiteSpace(requested)) return spa.DefaultReturnUrl;
    // Relative paths are always safe.
    if (Uri.IsWellFormedUriString(requested, UriKind.Relative)) return requested;
    if (!Uri.TryCreate(requested, UriKind.Absolute, out _)) return spa.DefaultReturnUrl;
    foreach (var allowed in spa.AllowedReturnUrlPrefixes)
    {
        if (!string.IsNullOrWhiteSpace(allowed)
            && requested.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
        {
            return requested;
        }
    }
    return spa.DefaultReturnUrl;
}

app.MapGet(BffRoutes.Login, (
    string? returnUrl,
    string? provider,
    Microsoft.Extensions.Options.IOptions<BffSpaOptions> spa,
    IIdentityProviderCatalog catalog) =>
{
    var props = new AuthenticationProperties { RedirectUri = ResolveReturnUrl(returnUrl, spa.Value) };
    // Caller-supplied provider aliases must pass through the catalog before being forwarded as
    // kc_idp_hint; otherwise an attacker can probe Keycloak with arbitrary broker aliases.
    if (!string.IsNullOrWhiteSpace(provider) && catalog.IsKnown(provider))
    {
        props.Items["kc_idp_hint"] = provider;
    }
    return Results.Challenge(props, [OpenIdConnectDefaults.AuthenticationScheme]);
});

app.MapGet(BffRoutes.Providers, (IIdentityProviderCatalog catalog) =>
    Results.Json(new { providers = catalog.List() }));

app.MapGet(BffRoutes.Logout, async (string? returnUrl, HttpContext ctx, Microsoft.Extensions.Options.IOptions<BffSpaOptions> spa) =>
{
    var target = ResolveReturnUrl(returnUrl, spa.Value);
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
    await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = target })
        .ConfigureAwait(false);
});

app.MapGet(BffRoutes.User, async (HttpContext ctx) =>
{
    if (ctx.User.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();
    // SaveTokens=true on the OIDC handler stashes the access token in the auth-cookie
    // ticket. Returning it here lets the SPA's apiClient send "Authorization: Bearer ..."
    // to the gateway's /api/{module}/* routes (which require the "authenticated" policy
    // and validate JWT bearer, not the BFF session cookie). Refresh handling is on the
    // followup list — for now, access tokens last ~5 min before module calls start 401-ing
    // again and the user has to re-sign in.
    var accessToken = await ctx.GetTokenAsync("access_token").ConfigureAwait(false);
    // Group by claim type — Keycloak emits multiple `roles` claims (one per role) and may
    // also emit duplicate `aud`/`amr` entries. A plain ToDictionary(c => c.Type, ...) would
    // throw "An item with the same key has already been added". Collapse single-value claim
    // types to a scalar string and multi-value types to a string[] so the SPA gets a clean
    // shape regardless of cardinality.
    var claims = ctx.User.Claims
        .GroupBy(c => c.Type, StringComparer.Ordinal)
        .ToDictionary(
            g => g.Key,
            g => g.Count() == 1 ? (object)g.First().Value : g.Select(c => c.Value).ToArray(),
            StringComparer.Ordinal);
    var realmAccessRoles = ctx.User.FindAll("realm_access").Select(c => c.Value).FirstOrDefault();
    var roles = ctx.User.FindAll("roles").Select(c => c.Value)
        .Concat(ctx.User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value))
        .Distinct(StringComparer.Ordinal)
        .ToArray();
    return Results.Json(new
    {
        name = ctx.User.Identity!.Name ?? ctx.User.FindFirst("preferred_username")?.Value,
        email = ctx.User.FindFirst("email")?.Value,
        roles,
        realm_access = realmAccessRoles,
        claims,
        accessToken,
    });
});

if (enableHisProxy)
    app.MapReverseProxy();

await app.RunAsync().ConfigureAwait(false);
