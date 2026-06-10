using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Dialysis.Identity.Bff.Configuration;
using Dialysis.Identity.Bff.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Dialysis.Identity.Tests.Bff;

/// <summary>
/// Locks down the BFF refresh-token flow. The service is wired to the cookie handler's
/// <c>OnValidatePrincipal</c> event, so its three exits (no expiry → return; not yet expiring →
/// return; expiring soon → refresh-or-reject) all need explicit coverage.
/// </summary>
public sealed class TokenRefreshServiceTests
{
    private static readonly DateTimeOffset _now = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Validate_Async_Is_A_Noop_When_No_Expires_At_Is_Saved_Async()
    {
        var http = new FakeHttp(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = BuildSut(http);
        var ctx = MakeContext(new Dictionary<string, string?>());

        await sut.ValidateAsync(ctx);

        ctx.Principal.ShouldNotBeNull();
        ctx.ShouldRenew.ShouldBeFalse();
        http.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Validate_Async_Does_Not_Refresh_When_Token_Is_Comfortably_Within_Window_Async()
    {
        var http = new FakeHttp(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = BuildSut(http);
        var futureExpiry = _now.AddMinutes(10).ToString("o", CultureInfo.InvariantCulture);
        var ctx = MakeContext(new Dictionary<string, string?>
        {
            [".Token.expires_at"] = futureExpiry,
            [".Token.refresh_token"] = "fake-refresh",
        });

        await sut.ValidateAsync(ctx);

        ctx.Principal.ShouldNotBeNull();
        ctx.ShouldRenew.ShouldBeFalse();
        http.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Validate_Async_Refreshes_When_Expiry_Is_Within_The_Skew_Window_Async()
    {
        var http = new FakeHttp(BuildOkResponse("new-access", "new-refresh", "new-id", 300));
        var sut = BuildSut(http);
        var nearExpiry = _now.AddSeconds(30).ToString("o", CultureInfo.InvariantCulture);
        var ctx = MakeContext(new Dictionary<string, string?>
        {
            [".Token.expires_at"] = nearExpiry,
            [".Token.refresh_token"] = "old-refresh",
            [".Token.access_token"] = "old-access",
            [".Token.id_token"] = "old-id",
        });

        await sut.ValidateAsync(ctx);

        ctx.Principal.ShouldNotBeNull();
        ctx.ShouldRenew.ShouldBeTrue();
        ctx.Properties.GetTokenValue("access_token").ShouldBe("new-access");
        ctx.Properties.GetTokenValue("refresh_token").ShouldBe("new-refresh");
        ctx.Properties.GetTokenValue("id_token").ShouldBe("new-id");
        var expiresAt = DateTimeOffset.Parse(ctx.Properties.GetTokenValue("expires_at")!, CultureInfo.InvariantCulture);
        expiresAt.ShouldBe(_now.AddSeconds(300), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Validate_Async_Rejects_The_Principal_When_Refresh_Token_Is_Absent_Async()
    {
        var http = new FakeHttp(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = BuildSut(http);
        var ctx = MakeContext(new Dictionary<string, string?>
        {
            [".Token.expires_at"] = _now.AddSeconds(10).ToString("o", CultureInfo.InvariantCulture),
        });

        await sut.ValidateAsync(ctx);

        ctx.Principal.ShouldBeNull();
        http.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Validate_Async_Rejects_The_Principal_When_Keycloak_Returns_An_Error_Async()
    {
        var http = new FakeHttp(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"invalid_grant\"}", Encoding.UTF8, "application/json"),
        });
        var sut = BuildSut(http);
        var ctx = MakeContext(new Dictionary<string, string?>
        {
            [".Token.expires_at"] = _now.AddSeconds(10).ToString("o", CultureInfo.InvariantCulture),
            [".Token.refresh_token"] = "stale-refresh",
        });

        await sut.ValidateAsync(ctx);

        ctx.Principal.ShouldBeNull();
        http.CallCount.ShouldBe(1);
    }

    private static TokenRefreshService BuildSut(FakeHttp http)
    {
        var factory = new SingleClientFactory(http);
        var options = Options.Create(new KeycloakBffOptions
        {
            Authority = "http://keycloak.test/realms/dialysis",
            ClientId = "dialysis-bff",
            ClientSecret = "test-secret",
        });
        var clock = new FixedClock(_now);
        return new TokenRefreshService(factory, options, clock, NullLogger<TokenRefreshService>.Instance);
    }

    private static CookieValidatePrincipalContext MakeContext(Dictionary<string, string?> items)
    {
        var properties = new AuthenticationProperties();
        foreach (var (k, v) in items)
            properties.Items[k] = v;
        var scheme = new AuthenticationScheme(
            CookieAuthenticationDefaults.AuthenticationScheme,
            displayName: null,
            handlerType: typeof(CookieAuthenticationHandler));
        var httpContext = new DefaultHttpContext { RequestServices = new EmptyProvider() };
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity("test")),
            properties,
            scheme.Name);
        return new CookieValidatePrincipalContext(httpContext, scheme, new CookieAuthenticationOptions(), ticket);
    }

    private static HttpResponseMessage BuildOkResponse(string access, string refresh, string id, int expiresIn)
    {
        var payload = JsonSerializer.Serialize(new
        {
            access_token = access,
            refresh_token = refresh,
            id_token = id,
            expires_in = expiresIn,
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
    }

    private sealed class FakeHttp : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public FakeHttp(HttpResponseMessage response) => _response = response;
        public int CallCount { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(_response);
        }
    }

    private sealed class SingleClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public SingleClientFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false)
        {
            DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Basic", "ignored") },
        };
    }

    private sealed class FixedClock : TimeProvider
    {
        private readonly DateTimeOffset _now1;
        public FixedClock(DateTimeOffset now) => _now1 = now;
        public override DateTimeOffset GetUtcNow() => _now1;
    }

    private sealed class EmptyProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
