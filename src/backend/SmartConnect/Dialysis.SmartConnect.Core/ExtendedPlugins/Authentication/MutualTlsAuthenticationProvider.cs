using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Dialysis.SmartConnect.Authentication;

namespace Dialysis.SmartConnect.ExtendedPlugins.Authentication;

/// <summary>
/// Slice A2: mutual-TLS provider (<c>Kind = "mutual-tls"</c>). Unlike the four header-based
/// providers from slice A, mTLS isn't a request-level mutation — the client certificate is
/// bound to the underlying <see cref="System.Net.Http.SocketsHttpHandler"/>. This provider
/// implements <see cref="ResolveClientAsync"/> instead, returning a pooled
/// <see cref="HttpClient"/> from <see cref="IMutualTlsHttpClientFactory"/> that already has
/// the cert attached. <see cref="ApplyAsync"/> is a no-op.
/// </summary>
/// <remarks>
/// Parameter shape (any one of <c>CertPath</c> / <c>PfxPath</c> required):
/// <code>
/// {
///   "Kind": "mutual-tls",
///   "CertPath": "/run/secrets/cerner-client.crt",      // PEM cert
///   "KeyPath":  "/run/secrets/cerner-client.key",      // PEM private key (matches CertPath)
///   "Password": "..."                                   // optional, when KeyPath is encrypted
/// }
/// </code>
/// Or, for PKCS#12 / PFX:
/// <code>
/// {
///   "Kind": "mutual-tls",
///   "PfxPath": "/run/secrets/epic-mtls.pfx",
///   "Password": "..."
/// }
/// </code>
/// </remarks>
public sealed class MutualTlsAuthenticationProvider : IHttpAuthenticationProvider
{
    private readonly IMutualTlsHttpClientFactory _factory;
    /// <summary>
    /// Slice A2: mutual-TLS provider (<c>Kind = "mutual-tls"</c>). Unlike the four header-based
    /// providers from slice A, mTLS isn't a request-level mutation — the client certificate is
    /// bound to the underlying <see cref="System.Net.Http.SocketsHttpHandler"/>. This provider
    /// implements <see cref="ResolveClientAsync"/> instead, returning a pooled
    /// <see cref="HttpClient"/> from <see cref="IMutualTlsHttpClientFactory"/> that already has
    /// the cert attached. <see cref="ApplyAsync"/> is a no-op.
    /// </summary>
    /// <remarks>
    /// Parameter shape (any one of <c>CertPath</c> / <c>PfxPath</c> required):
    /// <code>
    /// {
    ///   "Kind": "mutual-tls",
    ///   "CertPath": "/run/secrets/cerner-client.crt",      // PEM cert
    ///   "KeyPath":  "/run/secrets/cerner-client.key",      // PEM private key (matches CertPath)
    ///   "Password": "..."                                   // optional, when KeyPath is encrypted
    /// }
    /// </code>
    /// Or, for PKCS#12 / PFX:
    /// <code>
    /// {
    ///   "Kind": "mutual-tls",
    ///   "PfxPath": "/run/secrets/epic-mtls.pfx",
    ///   "Password": "..."
    /// }
    /// </code>
    /// </remarks>
    public MutualTlsAuthenticationProvider(IMutualTlsHttpClientFactory factory) => _factory = factory;
    public string Kind => "mutual-tls";

    public Task ApplyAsync(HttpRequestMessage request, string parametersJson, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<HttpClient?> ResolveClientAsync(
        string parametersJson,
        HttpClient defaultClient,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(defaultClient);
        var options = JsonSerializer.Deserialize<MutualTlsOptions>(parametersJson)
            ?? throw new InvalidOperationException("Mutual TLS authentication parameters must be a JSON object.");

        var cert = LoadCertificate(options);
        return Task.FromResult<HttpClient?>(_factory.GetClient(cert));
    }

    private static X509Certificate2 LoadCertificate(MutualTlsOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.PfxPath))
        {
            return string.IsNullOrEmpty(options.Password)
                ? X509CertificateLoader.LoadPkcs12FromFile(options.PfxPath, password: null)
                : X509CertificateLoader.LoadPkcs12FromFile(options.PfxPath, options.Password);
        }

        if (string.IsNullOrWhiteSpace(options.CertPath))
            throw new InvalidOperationException("Mutual TLS parameters must include either 'PfxPath' or 'CertPath'.");

        if (string.IsNullOrWhiteSpace(options.KeyPath))
            throw new InvalidOperationException("Mutual TLS parameters with 'CertPath' must also include 'KeyPath'.");

        return string.IsNullOrEmpty(options.Password)
            ? X509Certificate2.CreateFromPemFile(options.CertPath, options.KeyPath)
            : X509Certificate2.CreateFromEncryptedPemFile(options.CertPath, options.Password.AsSpan(), options.KeyPath);
    }

    private sealed class MutualTlsOptions
    {
        public string? CertPath { get; set; }

        public string? KeyPath { get; set; }

        public string? PfxPath { get; set; }

        public string? Password { get; set; }
    }
}
