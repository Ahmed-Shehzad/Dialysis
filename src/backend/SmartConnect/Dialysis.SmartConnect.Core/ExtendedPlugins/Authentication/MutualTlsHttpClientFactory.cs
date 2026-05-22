using System.Collections.Concurrent;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Dialysis.SmartConnect.Authentication;

namespace Dialysis.SmartConnect.ExtendedPlugins.Authentication;

/// <summary>
/// Default <see cref="IMutualTlsHttpClientFactory"/> — concurrent dictionary keyed by
/// certificate thumbprint. Each pool entry owns its <see cref="SocketsHttpHandler"/> and
/// the long-lived <see cref="HttpClient"/> sitting on top, so a partner with a stable
/// client certificate enjoys connection pooling across every outbound message. Cert
/// rotation works by deploying a new cert with a different thumbprint — the new client
/// gets a fresh pool entry, the old one ages out when no flow uses it.
/// </summary>
/// <remarks>
/// Implements <see cref="IDisposable"/> so the host can release the pooled clients on
/// shutdown; not <see cref="IAsyncDisposable"/> because <see cref="HttpClient"/> doesn't
/// implement it either.
/// </remarks>
public sealed class MutualTlsHttpClientFactory : IMutualTlsHttpClientFactory, IDisposable
{
    private readonly ConcurrentDictionary<string, HttpClient> _byThumbprint =
        new(StringComparer.OrdinalIgnoreCase);

    public HttpClient GetClient(X509Certificate2 clientCertificate)
    {
        ArgumentNullException.ThrowIfNull(clientCertificate);
        var thumbprint = clientCertificate.Thumbprint
            ?? throw new ArgumentException("Client certificate has no thumbprint.", nameof(clientCertificate));

        return _byThumbprint.GetOrAdd(thumbprint, _ => BuildClient(clientCertificate));
    }

    private static HttpClient BuildClient(X509Certificate2 cert)
    {
        var handler = new SocketsHttpHandler
        {
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                ClientCertificates = new X509CertificateCollection { cert },
            },
        };
        return new HttpClient(handler, disposeHandler: true);
    }

    public void Dispose()
    {
        foreach (var client in _byThumbprint.Values)
        {
            client.Dispose();
        }
        _byThumbprint.Clear();
    }
}
