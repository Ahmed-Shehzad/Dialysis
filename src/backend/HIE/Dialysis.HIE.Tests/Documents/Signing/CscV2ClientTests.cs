using System.Net;
using System.Text;
using System.Text.Json;
using Dialysis.BuildingBlocks.Documents.Signing.Csc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Documents.Signing;

/// <summary>
/// Exercises the CSC v2 client against a fake HttpMessageHandler. We don't need a real TSP
/// to confirm the wire shape — the handler replays the canonical responses for the three
/// endpoints we hit (OAuth2 token, /credentials/authorize, /signatures/signHash).
/// </summary>
public sealed class CscV2ClientTests
{
    [Fact]
    public async Task Sign_Hash_Async_Posts_To_Tsp_And_Returns_Decoded_Bytes_Async()
    {
        var pkcs7 = new byte[] { 0x30, 0x80, 0x06, 0x09 };
        var handler = new FakeHttpHandler(message =>
        {
            if (message.RequestUri!.AbsolutePath.EndsWith("/token", StringComparison.Ordinal))
            {
                return MakeJson(new { access_token = "token-xyz", expires_in = 3600 });
            }
            if (message.RequestUri!.AbsolutePath.EndsWith("/credentials/authorize", StringComparison.Ordinal))
            {
                return MakeJson(new { SAD = "sad-abc" });
            }
            if (message.RequestUri!.AbsolutePath.EndsWith("/signatures/signHash", StringComparison.Ordinal))
            {
                return MakeJson(new { signatures = new[] { Convert.ToBase64String(pkcs7) } });
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = MakeClient(handler);
        var hash = new byte[] { 1, 2, 3, 4 };

        var signature = await client.SignHashAsync("credential-1", hash, CancellationToken.None);

        signature.ShouldBe(pkcs7);
        handler.Calls.Select(c => c.RequestUri!.AbsolutePath).ShouldBe(
            ["/token", "/v2/credentials/authorize", "/v2/signatures/signHash"]);
    }

    [Fact]
    public async Task Sign_Hash_Async_Throws_When_Tsp_Returns_Empty_Signatures_Async()
    {
        var handler = new FakeHttpHandler(message =>
        {
            if (message.RequestUri!.AbsolutePath.EndsWith("/token", StringComparison.Ordinal))
                return MakeJson(new { access_token = "token-xyz", expires_in = 3600 });
            if (message.RequestUri!.AbsolutePath.EndsWith("/credentials/authorize", StringComparison.Ordinal))
                return MakeJson(new { SAD = "sad-abc" });
            return MakeJson(new { signatures = Array.Empty<string>() });
        });
        var client = MakeClient(handler);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            client.SignHashAsync("credential-1", new byte[] { 1 }, CancellationToken.None));
    }

    [Fact]
    public async Task Get_Credential_Info_Async_Decodes_Cert_Chain_Async()
    {
        var handler = new FakeHttpHandler(message =>
        {
            if (message.RequestUri!.AbsolutePath.EndsWith("/token", StringComparison.Ordinal))
                return MakeJson(new { access_token = "token-xyz", expires_in = 3600 });
            return MakeJson(new
            {
                cert = new
                {
                    certificates = new[] { Convert.ToBase64String(new byte[] { 0x01, 0x02 }) },
                    subjectDN = "CN=Test",
                    status = "VALID",
                },
                key = new { algo = new[] { "1.2.840.113549.1.1.11" }, len = 2048 },
            });
        });
        var client = MakeClient(handler);

        var info = await client.GetCredentialInfoAsync("credential-1", CancellationToken.None);

        info.Cert.ShouldNotBeNull();
        info.Cert!.SubjectDistinguishedName.ShouldBe("CN=Test");
        info.Cert.Certificates.Count.ShouldBe(1);
    }

    private static CscV2Client MakeClient(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://tsp.example/v2/") };
        var options = Options.Create(new CscV2Options
        {
            TspId = "fake-tsp",
            BaseUri = "https://tsp.example/v2",
            ClientCredentialsTokenUri = "https://tsp.example/token",
            ClientId = "client",
            ClientSecret = "secret",
        });
        return new CscV2Client(http, options, NoopDistributedCache.Instance, NullLogger<CscV2Client>.Instance);
    }

    private static HttpResponseMessage MakeJson(object payload) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
    };

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Calls { get; } = [];

        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request);
            return Task.FromResult(_handler(request));
        }
    }

    /// <summary>No-op <see cref="IDistributedCache"/> — the tests don't exercise caching.</summary>
    private sealed class NoopDistributedCache : IDistributedCache
    {
        public static readonly NoopDistributedCache Instance = new();
        public byte[]? Get(string key) => null;
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult<byte[]?>(null);
        public void Refresh(string key) { }
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
        public void Remove(string key) { }
        public Task RemoveAsync(string key, CancellationToken token = default) => Task.CompletedTask;
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) { }
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => Task.CompletedTask;
    }

}
