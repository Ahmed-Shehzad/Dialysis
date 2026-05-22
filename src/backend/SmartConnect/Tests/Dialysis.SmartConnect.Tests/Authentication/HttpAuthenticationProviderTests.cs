using System.Net;
using System.Text;
using Dialysis.SmartConnect.Authentication;
using Dialysis.SmartConnect.ExtendedPlugins.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Authentication;

/// <summary>
/// Slice A1 of the SmartConnect ↔ Mirth alignment plan: covers the four built-in
/// <see cref="IHttpAuthenticationProvider"/>s exposed via the generic <c>HttpOutboundAdapter</c>.
/// Per-provider tests assert request mutation; the registry test confirms case-insensitive lookup
/// and unknown-kind handling.
/// </summary>
public sealed class HttpAuthenticationProviderTests
{
    [Fact]
    public async Task Bearer_Token_Provider_Sets_Authorization_Header_Async()
    {
        var provider = new BearerTokenAuthenticationProvider();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://partner.example/api");
        await provider.ApplyAsync(request, """{"Kind":"bearer","Token":"abc.def.ghi"}""", CancellationToken.None);

        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("abc.def.ghi", request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task Bearer_Token_Provider_Rejects_Missing_Token_Async()
    {
        var provider = new BearerTokenAuthenticationProvider();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://partner.example/api");
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.ApplyAsync(request, """{"Kind":"bearer"}""", CancellationToken.None));
    }

    [Fact]
    public async Task Api_Key_Provider_Defaults_To_X_Api_Key_Header_Async()
    {
        var provider = new ApiKeyAuthenticationProvider();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://partner.example/api");
        await provider.ApplyAsync(request, """{"Kind":"api-key","Value":"secret-key-1"}""", CancellationToken.None);

        Assert.True(request.Headers.TryGetValues("X-API-Key", out var values));
        Assert.Equal("secret-key-1", Assert.Single(values));
    }

    [Fact]
    public async Task Api_Key_Provider_Honours_Custom_Header_Name_Async()
    {
        var provider = new ApiKeyAuthenticationProvider();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://partner.example/api");
        await provider.ApplyAsync(
            request,
            """{"Kind":"api-key","HeaderName":"Ocp-Apim-Subscription-Key","Value":"mayo-key-1"}""",
            CancellationToken.None);

        Assert.True(request.Headers.TryGetValues("Ocp-Apim-Subscription-Key", out var values));
        Assert.Equal("mayo-key-1", Assert.Single(values));
    }

    [Fact]
    public async Task Basic_Provider_Base64_Encodes_User_Colon_Password_Async()
    {
        var provider = new BasicAuthenticationProvider();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://partner.example/api");
        await provider.ApplyAsync(
            request,
            """{"Kind":"basic","Username":"alice","Password":"hunter2"}""",
            CancellationToken.None);

        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!));
        Assert.Equal("alice:hunter2", decoded);
    }

    [Fact]
    public async Task Oauth2_Provider_Fetches_Token_And_Caches_It_Async()
    {
        const string tokenJson = """{"access_token":"oauth-token-1","expires_in":600,"token_type":"Bearer"}""";
        var tokenEndpointCallCount = 0;
        var handler = new ScriptedHandler((req, _) =>
        {
            if (req.RequestUri!.AbsoluteUri == "https://idp.example/token")
            {
                Interlocked.Increment(ref tokenEndpointCallCount);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(tokenJson, Encoding.UTF8, "application/json"),
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var (httpClientFactory, cache) = BuildHttpInfrastructure(handler);

        var provider = new OAuth2ClientCredentialsAuthenticationProvider(httpClientFactory, cache);
        const string parameters = """{"Kind":"oauth2-client-credentials","TokenEndpoint":"https://idp.example/token","ClientId":"sc","ClientSecret":"shh","Scope":"read"}""";

        using (var first = new HttpRequestMessage(HttpMethod.Post, "https://partner.example/api"))
        {
            await provider.ApplyAsync(first, parameters, CancellationToken.None);
            Assert.Equal("Bearer", first.Headers.Authorization!.Scheme);
            Assert.Equal("oauth-token-1", first.Headers.Authorization.Parameter);
        }

        using (var second = new HttpRequestMessage(HttpMethod.Post, "https://partner.example/api"))
        {
            await provider.ApplyAsync(second, parameters, CancellationToken.None);
            Assert.Equal("oauth-token-1", second.Headers.Authorization!.Parameter);
        }

        Assert.Equal(1, tokenEndpointCallCount);
    }

    [Fact]
    public async Task Oauth2_Provider_Surfaces_Token_Endpoint_Errors_Async()
    {
        var handler = new ScriptedHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("""{"error":"invalid_client"}"""),
            });
        var (httpClientFactory, cache) = BuildHttpInfrastructure(handler);

        var provider = new OAuth2ClientCredentialsAuthenticationProvider(httpClientFactory, cache);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://partner.example/api");

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.ApplyAsync(
            request,
            """{"Kind":"oauth2-client-credentials","TokenEndpoint":"https://idp.example/token","ClientId":"sc","ClientSecret":"shh"}""",
            CancellationToken.None));
    }

    [Fact]
    public void Registry_Lookup_Is_Case_Insensitive_And_Reports_Unknown_Kinds()
    {
        var registry = new HttpAuthenticationProviderRegistry(
        [
            new BearerTokenAuthenticationProvider(),
            new ApiKeyAuthenticationProvider(),
        ]);

        Assert.True(registry.TryGet("Bearer", out var bearer));
        Assert.IsType<BearerTokenAuthenticationProvider>(bearer);
        Assert.True(registry.TryGet("API-KEY", out var apiKey));
        Assert.IsType<ApiKeyAuthenticationProvider>(apiKey);
        Assert.False(registry.TryGet("oauth2-client-credentials", out _));
        Assert.False(registry.TryGet(string.Empty, out _));
    }

    private static (IHttpClientFactory factory, IDistributedCache cache) BuildHttpInfrastructure(HttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddHttpClient("smartconnect-outbound").ConfigurePrimaryHttpMessageHandler(() => handler);
        var sp = services.BuildServiceProvider();
        return (sp.GetRequiredService<IHttpClientFactory>(), sp.GetRequiredService<IDistributedCache>());
    }

    /// <summary>
    /// Hand-rolled <see cref="HttpMessageHandler"/> so individual tests can script the response per
    /// request without dragging in a mocking library.
    /// </summary>
    private sealed class ScriptedHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> script) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(script(request, cancellationToken));
    }
}
