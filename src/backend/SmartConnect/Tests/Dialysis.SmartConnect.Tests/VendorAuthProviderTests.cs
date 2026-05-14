using System.Net;
using Dialysis.SmartConnect.Adapters;
using Dialysis.SmartConnect.Adapters.Allscripts;
using Dialysis.SmartConnect.Adapters.Cerner;
using Dialysis.SmartConnect.Adapters.Meditech;
using Dialysis.SmartConnect.Adapters.OpenEMR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class VendorAuthProviderTests
{
    private static readonly ExternalEhrContext _Context = new("t1", PatientLaunchContext: null);

    [Fact]
    public async Task Cerner_Posts_Client_Credentials_With_Basic_Auth_And_Caches_Async()
    {
        var (acquirer, handler) = BuildAcquirer();
        var sut = new CernerAuthProvider(
            Options.Create(new CernerAdapterOptions
            {
                BaseUrl = "https://fhir/cerner",
                TokenEndpoint = "https://auth/cerner/token",
                ClientId = "cid",
                ClientSecret = "csecret",
                Scope = "system/Patient.read",
            }),
            acquirer);

        var first = await sut.AcquireAccessTokenAsync(_Context, CancellationToken.None);
        var second = await sut.AcquireAccessTokenAsync(_Context, CancellationToken.None);

        Assert.Equal("opaque-token", first);
        Assert.Equal(first, second);
        Assert.Equal(1, handler.Invocations);
        Assert.Equal("Basic", handler.LastAuthScheme);
        Assert.Equal("cid:csecret", handler.LastBasicCredential);
        Assert.Equal("client_credentials", handler.LastForm["grant_type"]);
        Assert.Equal("system/Patient.read", handler.LastForm["scope"]);
    }

    [Fact]
    public async Task Meditech_Posts_Client_Credentials_With_Basic_Auth_Async()
    {
        var (acquirer, handler) = BuildAcquirer();
        var sut = new MeditechAuthProvider(
            Options.Create(new MeditechAdapterOptions
            {
                BaseUrl = "https://fhir/meditech",
                TokenEndpoint = "https://auth/meditech/token",
                ClientId = "mid",
                ClientSecret = "msecret",
            }),
            acquirer);

        var token = await sut.AcquireAccessTokenAsync(_Context, CancellationToken.None);

        Assert.Equal("opaque-token", token);
        Assert.Equal("Basic", handler.LastAuthScheme);
        Assert.Equal("mid:msecret", handler.LastBasicCredential);
    }

    [Fact]
    public async Task Open_Emr_Posts_Client_Credentials_In_Form_Not_Basic_Auth_Async()
    {
        var (acquirer, handler) = BuildAcquirer();
        var sut = new OpenEmrAuthProvider(
            Options.Create(new OpenEmrAdapterOptions
            {
                BaseUrl = "https://openemr/fhir",
                TokenEndpoint = "https://openemr/oauth2/default/token",
                ClientId = "oid",
                ClientSecret = "osecret",
                Scope = "system/Observation.read",
            }),
            acquirer);

        var token = await sut.AcquireAccessTokenAsync(_Context, CancellationToken.None);

        Assert.Equal("opaque-token", token);
        Assert.Null(handler.LastAuthScheme);
        Assert.Equal("oid", handler.LastForm["client_id"]);
        Assert.Equal("osecret", handler.LastForm["client_secret"]);
        Assert.Equal("system/Observation.read", handler.LastForm["scope"]);
    }

    [Fact]
    public async Task Allscripts_Posts_Password_Grant_With_App_Name_Header_Async()
    {
        var (acquirer, handler) = BuildAcquirer();
        var sut = new AllscriptsAuthProvider(
            Options.Create(new AllscriptsAdapterOptions
            {
                BaseUrl = "https://fhir/allscripts",
                TokenEndpoint = "https://auth/allscripts/token",
                AppName = "MyApp",
                ClientId = "acid",
                Username = "svc-user",
                Password = "svc-pass",
            }),
            acquirer);

        var token = await sut.AcquireAccessTokenAsync(_Context, CancellationToken.None);

        Assert.Equal("opaque-token", token);
        Assert.Equal("password", handler.LastForm["grant_type"]);
        Assert.Equal("acid", handler.LastForm["client_id"]);
        Assert.Equal("svc-user", handler.LastForm["username"]);
        Assert.Equal("svc-pass", handler.LastForm["password"]);
        Assert.Equal("MyApp", handler.LastAppNameHeader);
    }

    private static (OAuth2TokenAcquirer Acquirer, CapturingHandler Handler) BuildAcquirer()
    {
        var handler = new CapturingHandler();
        var factory = new SingleClientHttpClientFactory(handler);
        IDistributedCache cache = new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));
        var acquirer = new OAuth2TokenAcquirer(factory, cache, TimeSpan.FromSeconds(1));
        return (acquirer, handler);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public int Invocations { get; private set; }
        public string? LastAuthScheme { get; private set; }
        public string? LastBasicCredential { get; private set; }
        public string? LastAppNameHeader { get; private set; }
        public Dictionary<string, string> LastForm { get; private set; } = new(StringComparer.Ordinal);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Invocations++;
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            LastForm = body
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('='))
                .ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]), StringComparer.Ordinal);
            LastAuthScheme = request.Headers.Authorization?.Scheme;
            if (request.Headers.Authorization?.Parameter is { } encoded)
            {
                LastBasicCredential = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            }
            if (request.Headers.TryGetValues("AppName", out var values))
            {
                LastAppNameHeader = values.FirstOrDefault();
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"access_token":"opaque-token","expires_in":300,"token_type":"Bearer"}"""),
            };
        }
    }

    private sealed class SingleClientHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
