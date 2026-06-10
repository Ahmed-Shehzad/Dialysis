using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Dialysis.Identity.Bff.Configuration;
using Dialysis.Identity.Bff.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Dialysis.Identity.Tests.Bff;

/// <summary>
/// Locks down the RFC 8693 token-exchange flow that scopes the BFF session's Keycloak access
/// token to the HIS API audience: the unauthenticated/no-token short circuits, the exchange
/// request shape (grant type, audience, subject token, Basic client auth), the per-subject cache
/// that keeps Keycloak off the hot path, and the no-negative-caching behaviour on failure.
/// </summary>
public sealed class HisAccessTokenProviderTests
{
    [Fact]
    public async Task Returns_Null_When_There_Is_No_Http_Context_Async()
    {
        var http = new RecordingHandler(_ => OkExchangeResponse("exchanged"));
        var sut = BuildSut(http, new HttpContextAccessor { HttpContext = null });

        var token = await sut.GetAccessTokenForHisAsync(CancellationToken.None);

        token.ShouldBeNull();
        http.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Returns_Null_When_The_User_Is_Not_Authenticated_Async()
    {
        var http = new RecordingHandler(_ => OkExchangeResponse("exchanged"));
        var context = BuildHttpContext(accessToken: "session-token", authenticated: false);
        var sut = BuildSut(http, new HttpContextAccessor { HttpContext = context });

        var token = await sut.GetAccessTokenForHisAsync(CancellationToken.None);

        token.ShouldBeNull();
        http.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Returns_Null_When_The_Session_Has_No_Access_Token_Async()
    {
        var http = new RecordingHandler(_ => OkExchangeResponse("exchanged"));
        var context = BuildHttpContext(accessToken: null, authenticated: true);
        var sut = BuildSut(http, new HttpContextAccessor { HttpContext = context });

        var token = await sut.GetAccessTokenForHisAsync(CancellationToken.None);

        token.ShouldBeNull();
        http.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Exchanges_The_Session_Token_For_A_His_Scoped_Token_Async()
    {
        var http = new RecordingHandler(_ => OkExchangeResponse("his-scoped-token"));
        var context = BuildHttpContext(accessToken: "session-token", authenticated: true);
        var sut = BuildSut(http, new HttpContextAccessor { HttpContext = context });

        var token = await sut.GetAccessTokenForHisAsync(CancellationToken.None);

        token.ShouldBe("his-scoped-token");
        http.CallCount.ShouldBe(1);
        http.LastRequestUri.ShouldBe("http://keycloak.test/realms/dialysis/protocol/openid-connect/token");
        http.LastAuthorizationScheme.ShouldBe("Basic");
        var form = http.LastFormBody.ShouldNotBeNull();
        form.ShouldContain("grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Atoken-exchange");
        form.ShouldContain("subject_token=session-token");
        form.ShouldContain("audience=dialysis-his-api");
    }

    [Fact]
    public async Task Caches_The_Exchanged_Token_So_Repeat_Calls_Skip_Keycloak_Async()
    {
        var http = new RecordingHandler(_ => OkExchangeResponse("his-scoped-token"));
        var context = BuildHttpContext(accessToken: "session-token", authenticated: true);
        var sut = BuildSut(http, new HttpContextAccessor { HttpContext = context });

        var first = await sut.GetAccessTokenForHisAsync(CancellationToken.None);
        var second = await sut.GetAccessTokenForHisAsync(CancellationToken.None);

        first.ShouldBe("his-scoped-token");
        second.ShouldBe("his-scoped-token");
        http.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Returns_Null_And_Does_Not_Cache_When_Keycloak_Rejects_The_Exchange_Async()
    {
        var http = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"invalid_grant\"}", Encoding.UTF8, "application/json"),
        });
        var context = BuildHttpContext(accessToken: "session-token", authenticated: true);
        var sut = BuildSut(http, new HttpContextAccessor { HttpContext = context });

        var first = await sut.GetAccessTokenForHisAsync(CancellationToken.None);
        var second = await sut.GetAccessTokenForHisAsync(CancellationToken.None);

        first.ShouldBeNull();
        second.ShouldBeNull();
        http.CallCount.ShouldBe(2);
    }

    [Fact]
    public async Task Returns_Null_When_The_Response_Lacks_An_Access_Token_Async()
    {
        var http = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"token_type\":\"Bearer\"}", Encoding.UTF8, "application/json"),
        });
        var context = BuildHttpContext(accessToken: "session-token", authenticated: true);
        var sut = BuildSut(http, new HttpContextAccessor { HttpContext = context });

        var token = await sut.GetAccessTokenForHisAsync(CancellationToken.None);

        token.ShouldBeNull();
    }

    private static HisAccessTokenProvider BuildSut(RecordingHandler handler, IHttpContextAccessor accessor)
    {
        var options = Options.Create(new KeycloakBffOptions
        {
            Authority = "http://keycloak.test/realms/dialysis/",
            ClientId = "dialysis-bff",
            ClientSecret = "test-secret",
            HisAudienceClientId = "dialysis-his-api",
        });
        return new HisAccessTokenProvider(
            accessor,
            new SingleClientFactory(handler),
            options,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<HisAccessTokenProvider>.Instance);
    }

    private static DefaultHttpContext BuildHttpContext(string? accessToken, bool authenticated)
    {
        var identity = authenticated
            ? new ClaimsIdentity([new Claim("sub", "user-123")], authenticationType: "cookie")
            : new ClaimsIdentity();
        var services = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(new FakeAuthenticationService(accessToken))
            .BuildServiceProvider();
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
            RequestServices = services,
        };
    }

    private static HttpResponseMessage OkExchangeResponse(string accessToken)
    {
        var payload = JsonSerializer.Serialize(new { access_token = accessToken, expires_in = 300 });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
    }

    /// <summary>Serves the BFF cookie ticket (with or without a stashed access token).</summary>
    private sealed class FakeAuthenticationService : IAuthenticationService
    {
        private readonly string? _accessToken;
        public FakeAuthenticationService(string? accessToken) => _accessToken = accessToken;

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        {
            var properties = new AuthenticationProperties();
            if (_accessToken is not null)
                properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = _accessToken }]);
            var ticket = new AuthenticationTicket(context.User, properties, "cookie");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            throw new NotSupportedException();

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            throw new NotSupportedException();

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) =>
            throw new NotSupportedException();

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            throw new NotSupportedException();
    }

    /// <summary>Captures each exchange request (URI, auth scheme, form body) before replying.</summary>
    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        public int CallCount { get; private set; }
        public string? LastRequestUri { get; private set; }
        public string? LastAuthorizationScheme { get; private set; }
        public string? LastFormBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequestUri = request.RequestUri?.ToString();
            LastAuthorizationScheme = request.Headers.Authorization?.Scheme;
            if (request.Content is not null)
                LastFormBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return _respond(request);
        }
    }

    private sealed class SingleClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public SingleClientFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }
}
