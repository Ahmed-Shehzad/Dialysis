using System.Net.Http.Headers;
using Dialysis.BuildingBlocks.DistributedCache.Valkey;
using Dialysis.Identity.Bff.Configuration;
using Dialysis.Identity.Bff.Federation;
using Dialysis.Identity.Bff.Services;
using Dialysis.Module.Bff;
using Dialysis.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Dialysis.Identity.Bff;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddAuthorization();

// Server-side cookie ticket store (see DistributedCacheTicketStore): keeps the SaveTokens=true
// Keycloak token bundle off the browser cookie so the BFF cookie stays a short session key on the
// shared gateway origin. Valkey when configured (also wires the Data Protection key ring for
// multi-replica cookie decryption), in-memory distributed cache as the dev fallback.
var idValkeySection = builder.Configuration.GetSection("Bff:DistributedCache:Valkey");
if (!string.IsNullOrWhiteSpace(idValkeySection["ConnectionString"]))
{
    if (string.IsNullOrWhiteSpace(idValkeySection["InstanceName"]))
        idValkeySection["InstanceName"] = "identity-bff";
    builder.Services.AddValkeyDistributedCache(idValkeySection);
}
else
{
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddSingleton<ITicketStore>(sp =>
    new DistributedCacheTicketStore(sp.GetRequiredService<IDistributedCache>(), "Dialysis.Identity.Bff:ticket:"));
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<ITicketStore>((o, store) => o.SessionStore = store);
builder.Services.Configure<KeycloakBffOptions>(builder.Configuration.GetSection(KeycloakBffOptions.SectionName));
builder.Services.Configure<BffSpaOptions>(builder.Configuration.GetSection(BffSpaOptions.SectionName));
builder.Services.Configure<IdentityFederationOptions>(builder.Configuration.GetSection(IdentityFederationOptions.SectionName));
builder.Services.AddSingleton<IIdentityProviderCatalog, ConfiguredIdentityProviderCatalog>();
builder.Services.AddHttpClient("keycloak");
builder.Services.AddScoped<IHisAccessTokenProvider, HisAccessTokenProvider>();
builder.Services.AddSingleton<ITokenRefreshService, TokenRefreshService>();
builder.Services.AddSingleton(TimeProvider.System);

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
        // Path-scope the cookie to this BFF's base (/identity) so it isn't sent on every other
        // context's path on the shared gateway origin. Without this the legacy cookie rides on
        // /smartconnect, /ehr, … and piles onto each context's own cookie — the accumulation that
        // overflowed Kestrel's header limit and produced HTTP 431 on plain SPA loads.
        o.Cookie.Path = BffRoutes.Base;
        o.Cookie.SameSite = SameSiteMode.Lax;
        // Hand cookie validation off to ITokenRefreshService so the cached Keycloak access
        // token rolls forward before it expires. Without this, the BFF session cookie outlives
        // the access token by minutes/hours and gateway-routed /api/* calls 401 silently until
        // the user re-signs in.
        o.Events.OnValidatePrincipal = async ctx =>
        {
            var refresher = ctx.HttpContext.RequestServices.GetRequiredService<ITokenRefreshService>();
            await refresher.ValidateAsync(ctx).ConfigureAwait(false);
        };
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
        // offline_access opts the auth-code grant into issuing a refresh_token alongside the
        // access token; without it Keycloak omits refresh_token and the BFF can't roll the
        // session forward when ITokenRefreshService runs.
        o.Scope.Add("offline_access");

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
    if (string.IsNullOrWhiteSpace(requested))
        return spa.DefaultReturnUrl;
    // Relative paths are always safe.
    if (Uri.IsWellFormedUriString(requested, UriKind.Relative))
        return requested;
    if (!Uri.TryCreate(requested, UriKind.Absolute, out _))
        return spa.DefaultReturnUrl;
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
    // and validate JWT bearer, not the BFF session cookie). ITokenRefreshService rolls the
    // saved tokens forward before they expire so this stays a current access token across
    // long-lived sessions.
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
    // Permissions surface as one or more `dialysis_permission` claims (the realm mapper emits
    // a JSON-typed claim; multi-valued claims arrive as repeated claims, each containing the
    // full array literal). Flatten and parse so the SPA's PermissionGate can do a simple
    // `permissions.includes(required)` check. If the claim is absent (no permission mapper
    // configured for the upstream IdP), the array is empty and PermissionGate falls back to
    // hiding any permission-gated UI.
    var permissions = ExtractPermissions(ctx.User.FindAll("dialysis_permission").Select(c => c.Value));
    return Results.Json(new
    {
        name = ctx.User.Identity!.Name ?? ctx.User.FindFirst("preferred_username")?.Value,
        email = ctx.User.FindFirst("email")?.Value,
        roles,
        realm_access = realmAccessRoles,
        permissions,
        claims,
        accessToken,
    });
});

static string[] ExtractPermissions(IEnumerable<string> rawValues)
{
    var output = new HashSet<string>(StringComparer.Ordinal);
    foreach (var raw in rawValues)
    {
        if (string.IsNullOrWhiteSpace(raw))
            continue;
        var trimmed = raw.AsSpan().Trim();
        // Each claim value is either a JSON array literal (`["a","b"]`) when the mapper is
        // jsonType=JSON, or a single scalar string when scope mappers add one permission per
        // claim. Detect the array shape and parse it; otherwise treat the value as a single
        // permission. Malformed JSON falls through to the scalar path so a misconfigured
        // upstream IdP can't poison the principal.
        if (trimmed.Length >= 2 && trimmed[0] == '[' && trimmed[^1] == ']')
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(raw);
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        var value = element.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            output.Add(value);
                    }
                    continue;
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // fall through to scalar handling
            }
        }
        output.Add(raw);
    }
    return [.. output];
}

if (enableHisProxy)
    app.MapReverseProxy();

await app.RunAsync().ConfigureAwait(false);
