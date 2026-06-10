using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using Dialysis.SmartConnect.Adapters;
using Dialysis.SmartConnect.Adapters.Epic;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class EpicAuthProviderTests
{
    [Fact]
    public async Task Acquires_Token_With_Signed_Jwt_Assertion_And_Caches_Result_Async()
    {
        using var pem = new TempPemFile();
        var options = Options.Create(new EpicAdapterOptions
        {
            BaseUrl = "https://example/FHIR/R4",
            TokenEndpoint = "https://example/oauth2/token",
            ClientId = "test-client",
            PrivateKeyPemPath = pem.Path,
            Scope = "system/Patient.read",
        });

        var capturingHandler = new CapturingHandler();
        var factory = new SingleClientHttpClientFactory(capturingHandler);
        IDistributedCache cache = new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions()));
        var acquirer = new OAuth2TokenAcquirer(factory, cache, TimeSpan.FromSeconds(1));
        var sut = new EpicAuthProvider(options, acquirer);

        var context = new ExternalEhrContext(TenantId: "t1", PatientLaunchContext: null);

        var first = await sut.AcquireAccessTokenAsync(context, CancellationToken.None);
        var second = await sut.AcquireAccessTokenAsync(context, CancellationToken.None);

        Assert.Equal("opaque-token", first);
        Assert.Equal(first, second);
        Assert.Equal(1, capturingHandler.Invocations);

        var assertion = capturingHandler.LastClientAssertion;
        Assert.False(string.IsNullOrEmpty(assertion));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(assertion);
        Assert.Equal("test-client", jwt.Issuer);
        Assert.Equal("https://example/oauth2/token", jwt.Audiences.Single());
        Assert.Equal("RS384", jwt.SignatureAlgorithm);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public int Invocations { get; private set; }
        public string? LastClientAssertion { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Invocations++;
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            var pairs = body.Split('&').Select(p => p.Split('=')).ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));
            LastClientAssertion = pairs["client_assertion"];
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"access_token":"opaque-token","expires_in":300,"token_type":"Bearer"}"""),
            };
            return response;
        }
    }

    private sealed class SingleClientHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public SingleClientHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private sealed class TempPemFile : IDisposable
    {
        public string Path { get; }

        public TempPemFile()
        {
            using var rsa = RSA.Create(2048);
            var pem = rsa.ExportRSAPrivateKeyPem();
            Path = System.IO.Path.GetTempFileName();
            File.WriteAllText(Path, pem);
        }

        public void Dispose()
        {
            try
            { File.Delete(Path); }
            catch { /* best effort */ }
        }
    }
}
