using System.Net.Http.Headers;
using System.Security.Claims;
using Dialysis.Identity.Bff.Configuration;
using Microsoft.AspNetCore.Authentication;
using Dialysis.Identity.Bff.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddAuthorization();
builder.Services.Configure<KeycloakBffOptions>(builder.Configuration.GetSection(KeycloakBffOptions.SectionName));
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
        o.Scope.Add("openid");
        o.Scope.Add("profile");
        o.Scope.Add("email");
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

app.MapGet("/", () => Results.Text(
    "Dialysis Identity BFF — use GET /login to sign in, GET /user for claims, /his/... to proxy to HIS when ReverseProxy is configured.",
    "text/plain"));

app.MapGet("/login", () =>
    Results.Challenge(
        new AuthenticationProperties { RedirectUri = "/" },
        [OpenIdConnectDefaults.AuthenticationScheme]));

app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
    await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = "/" })
        .ConfigureAwait(false);
});

app.MapGet("/user", (HttpContext ctx) =>
{
    if (ctx.User.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();
    var payload = ctx.User.Claims.Select(c => new { c.Type, c.Value }).ToList();
    return Results.Json(payload);
});

if (enableHisProxy)
    app.MapReverseProxy();

await app.RunAsync().ConfigureAwait(false);
