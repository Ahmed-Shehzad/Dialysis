using System.Net.Http.Headers;
using System.Text.Json;
using Dialysis.BuildingBlocks.DistributedCache.Valkey;
using Dialysis.BuildingBlocks.Transponder.Hosting;
using Dialysis.Module.Bff.Configuration;
using Dialysis.Module.Bff.Federation;
using Dialysis.Module.Bff.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Dialysis.Module.Bff;

/// <summary>
/// One-call registration for a bounded-context Backend-for-Frontend. Call
/// <see cref="AddModuleBff"/> on the host builder and <see cref="MapModuleBff"/> on the app —
/// the BFF then serves <c>{base}/identity/{login,logout,user,providers,signin-oidc}</c> and (when a
/// module API address is configured) proxies <c>{base}/api/*</c> + <c>{base}/hubs/*</c> to it,
/// attaching the session's bearer token.
/// </summary>
public static class ModuleBffExtensions
{
    /// <summary>Registers OIDC + cookie auth, federation catalog, token refresh, and the module proxy.</summary>
    public static WebApplicationBuilder AddModuleBff(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var config = builder.Configuration;

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddMemoryCache();
        builder.Services.AddAuthorization();
        builder.Services.Configure<ModuleBffOptions>(config.GetSection(ModuleBffOptions.SectionName));
        builder.Services.Configure<KeycloakBffOptions>(config.GetSection(KeycloakBffOptions.SectionName));
        builder.Services.Configure<BffSpaOptions>(config.GetSection(BffSpaOptions.SectionName));
        builder.Services.Configure<IdentityFederationOptions>(config.GetSection(IdentityFederationOptions.SectionName));
        builder.Services.AddSingleton<IIdentityProviderCatalog, ConfiguredIdentityProviderCatalog>();
        builder.Services.AddHttpClient("keycloak");
        builder.Services.AddSingleton<ITokenRefreshService, TokenRefreshService>();
        builder.Services.AddSingleton(TimeProvider.System);

        var module = config.GetSection(ModuleBffOptions.SectionName).Get<ModuleBffOptions>() ?? new ModuleBffOptions();
        var routes = new BffRoutePaths(module.ResolveBasePath());
        builder.Services.AddSingleton(routes);

        // Server-side cookie ticket store. SaveTokens=true (below) keeps the Keycloak access/id/
        // refresh bundle on the auth ticket; without a session store the cookie middleware packs that
        // bundle into the browser cookie and chunks it across .CookieC1/C2/…, and on the single shared
        // gateway origin those per-context cookies accumulate past Kestrel's 32 KB header limit → 431.
        // Backing the ticket store with Valkey (in-memory distributed cache as a dev fallback) keeps
        // only a session key in the cookie and lets the store survive across BFF replicas; the same
        // call wires the Data Protection key ring so the cookie itself decrypts on any replica.
        var valkeySection = config.GetSection("Bff:DistributedCache:Valkey");
        if (!string.IsNullOrWhiteSpace(valkeySection["ConnectionString"]))
        {
            if (string.IsNullOrWhiteSpace(valkeySection["InstanceName"]))
                valkeySection["InstanceName"] = string.IsNullOrWhiteSpace(module.Slug) ? "bff" : $"{module.Slug}-bff";
            builder.Services.AddValkeyDistributedCache(valkeySection);
        }
        else
        {
            builder.Services.AddDistributedMemoryCache();
        }

        var ticketKeyPrefix = module.ResolveCookieName() + ":ticket:";
        builder.Services.AddSingleton<ITicketStore>(sp =>
            new DistributedCacheTicketStore(sp.GetRequiredService<IDistributedCache>(), ticketKeyPrefix));
        builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((o, store) => o.SessionStore = store);

        var kc = config.GetSection(KeycloakBffOptions.SectionName).Get<KeycloakBffOptions>() ?? new KeycloakBffOptions();
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
                o.Cookie.Name = module.ResolveCookieName();
                // Path-scope the cookie to this context so per-context sessions never collide on
                // the single shared gateway origin (his cookie is not sent to /ehr, etc.).
                o.Cookie.Path = routes.Base[..routes.Base.LastIndexOf('/')] is { Length: > 0 } p ? p : "/";
                o.Cookie.SameSite = SameSiteMode.Lax;
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
                o.CallbackPath = routes.SignInCallback;
                o.SignedOutCallbackPath = routes.SignedOutCallback;
                o.CorrelationCookie.SameSite = SameSiteMode.Lax;
                o.NonceCookie.SameSite = SameSiteMode.Lax;
                o.Scope.Add("openid");
                o.Scope.Add("profile");
                o.Scope.Add("email");
                o.Scope.Add("offline_access");
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

        if (!string.IsNullOrWhiteSpace(module.ModuleApiAddress)
            && Uri.TryCreate(module.ModuleApiAddress, UriKind.Absolute, out _))
        {
            var basePath = module.ResolveBasePath();
            var apiRoute = new RouteConfig
            {
                RouteId = $"{module.Slug}-api",
                ClusterId = "module",
                Match = new RouteMatch { Path = basePath + "/api/{**remainder}" },
                Transforms = [new Dictionary<string, string> { ["PathRemovePrefix"] = basePath }],
            };
            var hubRoute = new RouteConfig
            {
                RouteId = $"{module.Slug}-hubs",
                ClusterId = "module",
                Match = new RouteMatch { Path = basePath + "/hubs/{**remainder}" },
                Transforms = [new Dictionary<string, string> { ["PathRemovePrefix"] = basePath }],
            };
            var cluster = new ClusterConfig
            {
                ClusterId = "module",
                Destinations = new Dictionary<string, DestinationConfig>(StringComparer.Ordinal)
                {
                    ["d1"] = new DestinationConfig { Address = module.ModuleApiAddress },
                },
            };

            var routeList = new List<RouteConfig> { apiRoute, hubRoute };
            var clusterList = new List<ClusterConfig> { cluster };

            // Cross-context aggregations: {base}/api/_x/{key}/{rest} → {upstream}/api/{rest}. More
            // specific than the catch-all {base}/api/{**}, and given a lower Order so YARP prefers it.
            foreach (var agg in module.Aggregations)
            {
                if (string.IsNullOrWhiteSpace(agg.Key) || string.IsNullOrWhiteSpace(agg.Address)
                    || !Uri.TryCreate(agg.Address, UriKind.Absolute, out _))
                {
                    continue;
                }
                var prefix = $"{basePath}/api/_x/{agg.Key}";
                routeList.Add(new RouteConfig
                {
                    RouteId = $"{module.Slug}-agg-{agg.Key}",
                    ClusterId = $"agg-{agg.Key}",
                    Order = -1,
                    Match = new RouteMatch { Path = prefix + "/{**remainder}" },
                    // Strip the aggregation prefix and forward the remainder verbatim — the SPA
                    // supplies the upstream module's exact path (e.g. /api/v1.0/… or /admin/…)
                    // after _x/{key}, so a single SPA-side rewrite of /api/{key}/ → {base}/api/_x/{key}/
                    // works regardless of where the endpoint is mounted on the upstream.
                    Transforms = [new Dictionary<string, string> { ["PathRemovePrefix"] = prefix }],
                });
                clusterList.Add(new ClusterConfig
                {
                    ClusterId = $"agg-{agg.Key}",
                    Destinations = new Dictionary<string, DestinationConfig>(StringComparer.Ordinal)
                    {
                        ["d1"] = new DestinationConfig { Address = agg.Address },
                    },
                });
            }

            builder.Services.AddReverseProxy()
                .LoadFromMemory(routeList, clusterList)
                .AddTransforms(transforms =>
                {
                    // Attach the session's Keycloak access token (kept on the cookie ticket by
                    // SaveTokens=true and rolled forward by ITokenRefreshService) so the owning
                    // module API — which validates JWT bearer, not the BFF cookie — authorizes.
                    transforms.AddRequestTransform(async context =>
                    {
                        var token = await context.HttpContext.GetTokenAsync("access_token").ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                            return;
                        }

                        // No cookie session. In dev only (AllowServiceBearerPassthrough), forward an inbound
                        // service-account bearer so the data simulator can drive module writes through the BFF.
                        if (module.AllowServiceBearerPassthrough
                            && context.HttpContext.Request.Headers.TryGetValue("Authorization", out var inbound)
                            && inbound.Count > 0
                            && inbound[0] is { } raw
                            && raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            context.ProxyRequest.Headers.Authorization =
                                new AuthenticationHeaderValue("Bearer", raw["Bearer ".Length..].Trim());
                        }
                    });
                });
        }

        // Transponder Hangfire scheduler — PostgreSQL-backed background scheduling. The AppHost injects
        // ConnectionStrings:Hangfire (the corresponding module database); it is absent in tests.
        var hangfireConnectionString = builder.Configuration.GetConnectionString("Hangfire");
        if (!string.IsNullOrWhiteSpace(hangfireConnectionString))
            builder.Services.AddTransponderHangfire(hangfireConnectionString);

        return builder;
    }

    /// <summary>Maps the auth endpoints and (when configured) the module reverse proxy.</summary>
    public static WebApplication MapModuleBff(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var routes = app.Services.GetRequiredService<BffRoutePaths>();
        var module = app.Services.GetRequiredService<IOptions<ModuleBffOptions>>().Value;

        app.UseAuthentication();
        app.UseAuthorization();

        // Hangfire dashboard at /hangfire (no-op unless Hangfire is configured for this BFF).
        app.UseModuleHangfireDashboard($"{module.Slug} BFF");

        app.MapGet(routes.Root, () => Results.Text(
            $"Dialysis {module.Slug} BFF — GET {routes.Login} to sign in, GET {routes.User} for claims.",
            "text/plain"));

        app.MapGet(routes.Login, (
            string? returnUrl,
            string? provider,
            IOptions<BffSpaOptions> spa,
            IIdentityProviderCatalog catalog) =>
        {
            var props = new AuthenticationProperties { RedirectUri = ResolveReturnUrl(returnUrl, spa.Value) };
            if (!string.IsNullOrWhiteSpace(provider) && catalog.IsKnown(provider))
                props.Items["kc_idp_hint"] = provider;
            return Results.Challenge(props, [OpenIdConnectDefaults.AuthenticationScheme]);
        });

        app.MapGet(routes.Providers, (IIdentityProviderCatalog catalog) =>
            Results.Json(new { providers = catalog.List() }));

        app.MapGet(routes.Logout, async (string? returnUrl, HttpContext ctx, IOptions<BffSpaOptions> spa) =>
        {
            var target = ResolveReturnUrl(returnUrl, spa.Value);
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
            await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
                    new AuthenticationProperties { RedirectUri = target })
                .ConfigureAwait(false);
        });

        app.MapGet(routes.User, async (HttpContext ctx) =>
        {
            if (ctx.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();
            var accessToken = await ctx.GetTokenAsync("access_token").ConfigureAwait(false);
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

        if (!string.IsNullOrWhiteSpace(module.ModuleApiAddress)
            && Uri.TryCreate(module.ModuleApiAddress, UriKind.Absolute, out _))
        {
            app.MapReverseProxy();
        }

        return app;
    }

    private static string ResolveReturnUrl(string? requested, BffSpaOptions spa)
    {
        if (string.IsNullOrWhiteSpace(requested))
            return spa.DefaultReturnUrl;
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

    internal static string[] ExtractPermissions(IEnumerable<string> rawValues)
    {
        var output = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in rawValues)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            var trimmed = raw.AsSpan().Trim();
            if (trimmed.Length >= 2 && trimmed[0] == '[' && trimmed[^1] == ']')
            {
                try
                {
                    using var doc = JsonDocument.Parse(raw);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
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
                catch (JsonException)
                {
                    // fall through to scalar handling
                }
            }
            output.Add(raw);
        }
        return [.. output];
    }
}
