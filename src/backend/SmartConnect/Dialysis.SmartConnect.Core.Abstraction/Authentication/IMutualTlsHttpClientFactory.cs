using System.Security.Cryptography.X509Certificates;

namespace Dialysis.SmartConnect.Authentication;

/// <summary>
/// Slice A2 port: hands back an <see cref="HttpClient"/> whose underlying handler has the
/// supplied <see cref="X509Certificate2"/> attached as a client certificate. The factory
/// caches one client per certificate thumbprint so partner pools stay bounded and TLS
/// session reuse is preserved — certificates rotate maybe yearly, requests fly through
/// hundreds of times per second.
/// </summary>
public interface IMutualTlsHttpClientFactory
{
    HttpClient GetClient(X509Certificate2 clientCertificate);
}
