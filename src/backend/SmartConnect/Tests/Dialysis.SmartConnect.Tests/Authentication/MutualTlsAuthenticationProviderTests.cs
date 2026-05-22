using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Dialysis.SmartConnect.Authentication;
using Dialysis.SmartConnect.ExtendedPlugins.Authentication;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Authentication;

/// <summary>
/// Slice A2: mutual-TLS provider returns a pooled <see cref="HttpClient"/> with the
/// configured client certificate attached. Header-based providers from slice A still
/// participate through <see cref="IHttpAuthenticationProvider.ApplyAsync"/>; mTLS uses
/// the new <see cref="IHttpAuthenticationProvider.ResolveClientAsync"/> hook because the
/// cert is bound to the underlying handler, not the request.
/// </summary>
public sealed class MutualTlsAuthenticationProviderTests
{
    [Fact]
    public async Task Mutual_Tls_Provider_Resolves_Pooled_Client_For_The_Cert_Async()
    {
        var (certPath, keyPath) = WriteEphemeralCert();
        try
        {
            using var factory = new MutualTlsHttpClientFactory();
            var provider = new MutualTlsAuthenticationProvider(factory);
            using var defaultClient = new HttpClient();
            var parametersJson = $$"""
                {
                  "Kind": "mutual-tls",
                  "CertPath": "{{certPath.Replace("\\", "/")}}",
                  "KeyPath":  "{{keyPath.Replace("\\", "/")}}"
                }
                """;

            var resolved = await provider.ResolveClientAsync(parametersJson, defaultClient, CancellationToken.None);

            Assert.NotNull(resolved);
            Assert.NotSame(defaultClient, resolved);
        }
        finally
        {
            File.Delete(certPath);
            File.Delete(keyPath);
        }
    }

    [Fact]
    public async Task Mutual_Tls_Provider_Caches_Per_Cert_Thumbprint_Async()
    {
        var (certPath, keyPath) = WriteEphemeralCert();
        try
        {
            using var factory = new MutualTlsHttpClientFactory();
            var provider = new MutualTlsAuthenticationProvider(factory);
            using var defaultClient = new HttpClient();
            var parametersJson = $$"""
                {
                  "Kind": "mutual-tls",
                  "CertPath": "{{certPath.Replace("\\", "/")}}",
                  "KeyPath":  "{{keyPath.Replace("\\", "/")}}"
                }
                """;

            var first = await provider.ResolveClientAsync(parametersJson, defaultClient, CancellationToken.None);
            var second = await provider.ResolveClientAsync(parametersJson, defaultClient, CancellationToken.None);

            Assert.Same(first, second);
        }
        finally
        {
            File.Delete(certPath);
            File.Delete(keyPath);
        }
    }

    [Fact]
    public async Task Mutual_Tls_Provider_Apply_Is_A_No_Op_Async()
    {
        using var factory = new MutualTlsHttpClientFactory();
        var provider = new MutualTlsAuthenticationProvider(factory);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://partner.example/api");

        // ApplyAsync must not mutate the request — the cert is attached to the handler
        // returned by ResolveClientAsync, not the per-request headers.
        await provider.ApplyAsync(request, """{"Kind":"mutual-tls"}""", CancellationToken.None);

        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public async Task Mutual_Tls_Provider_Rejects_Pem_Without_Key_Path_Async()
    {
        using var factory = new MutualTlsHttpClientFactory();
        var provider = new MutualTlsAuthenticationProvider(factory);
        using var defaultClient = new HttpClient();

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.ResolveClientAsync(
            """{"Kind":"mutual-tls","CertPath":"/tmp/cert.pem"}""",
            defaultClient,
            CancellationToken.None));
    }

    [Fact]
    public async Task Mutual_Tls_Provider_Rejects_Parameters_With_Neither_Cert_Nor_Pfx_Async()
    {
        using var factory = new MutualTlsHttpClientFactory();
        var provider = new MutualTlsAuthenticationProvider(factory);
        using var defaultClient = new HttpClient();

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.ResolveClientAsync(
            """{"Kind":"mutual-tls"}""",
            defaultClient,
            CancellationToken.None));
    }

    /// <summary>
    /// Writes a fresh self-signed cert + PKCS#8 unencrypted key to the temp folder so the
    /// provider's PEM loader has something real to chew on. Returns the two paths; the
    /// caller cleans up.
    /// </summary>
    private static (string CertPath, string KeyPath) WriteEphemeralCert()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=smartconnect-test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(7));

        var certPath = Path.GetTempFileName();
        var keyPath = Path.GetTempFileName();
        File.WriteAllText(certPath, cert.ExportCertificatePem());
        File.WriteAllText(keyPath, rsa.ExportPkcs8PrivateKeyPem());
        return (certPath, keyPath);
    }
}
