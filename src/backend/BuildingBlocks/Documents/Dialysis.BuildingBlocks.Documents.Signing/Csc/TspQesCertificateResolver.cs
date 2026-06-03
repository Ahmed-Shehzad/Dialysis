using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Caching.Memory;

namespace Dialysis.BuildingBlocks.Documents.Signing.Csc;

/// <summary>
/// Resolver for the eIDAS-QES path. Returns the <em>public</em> cert for the requested
/// TSP credential — the private key never leaves the TSP. The PDF signer pairs this with
/// <see cref="IRemoteSignatureService"/> to delegate the actual hash signing over CSC v2.
///
/// The cert chain is cached in-memory per credentialId so repeated signs with the same
/// credential don't round-trip the TSP on every request.
/// </summary>
public sealed class TspQesCertificateResolver : ISigningCertificateResolver
{
    private readonly CscV2Client _client;
    private readonly IMemoryCache _cache;

    public TspQesCertificateResolver(CscV2Client client, IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(cache);
        _client = client;
        _cache = cache;
    }

    public PdfSigningCertificateSource Source => PdfSigningCertificateSource.RemoteQes;

    public async Task<X509Certificate2> ResolveAsync(PdfSigningRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.TspCredentialId))
        {
            throw new InvalidOperationException(
                "PdfSigningRequest.TspCredentialId is required for the RemoteQes path.");
        }
        var key = $"csc-qes-cert:{_client.TspId}:{request.TspCredentialId}";
        if (_cache.TryGetValue<X509Certificate2>(key, out var cached) && cached is not null)
        {
            return cached;
        }
        var info = await _client.GetCredentialInfoAsync(request.TspCredentialId, cancellationToken).ConfigureAwait(false);
        if (info.Cert is null || info.Cert.Certificates.Count == 0)
        {
            throw new InvalidOperationException($"TSP returned no certificates for credential '{request.TspCredentialId}'.");
        }
        // CSC v2: certificates are base64-encoded DER; element [0] is the end-entity cert.
        var endEntityDer = Convert.FromBase64String(info.Cert.Certificates[0]);
        var certificate = X509CertificateLoader.LoadCertificate(endEntityDer);
        _cache.Set(key, certificate, TimeSpan.FromHours(1));
        return certificate;
    }
}
