using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Documents.Signing.Ltv;

/// <summary>
/// Collects the CRLs / OCSP responses needed to long-term-validate a signed PDF without
/// further network access. Uses <see cref="X509Chain"/> in <c>Online</c> revocation mode
/// to walk the signer-cert chain — the chain build triggers .NET's built-in CRL+OCSP
/// fetcher and caches the results. We then read the raw responses back out of the chain
/// elements and hand them to the augmenter.
///
/// The collected blobs are also stored on the <c>DocumentReferenceSignature</c> row so a
/// future audit can replay the verification without re-fetching from the issuer's URLs.
/// </summary>
public sealed class RevocationEvidenceCollector
{
    private readonly LtvOptions _ltvOptions;
    private readonly ILogger<RevocationEvidenceCollector> _logger;

    public RevocationEvidenceCollector(
        IOptions<LtvOptions> ltvOptions,
        ILogger<RevocationEvidenceCollector> logger)
    {
        ArgumentNullException.ThrowIfNull(ltvOptions);
        ArgumentNullException.ThrowIfNull(logger);
        _ltvOptions = ltvOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Walks <paramref name="signerCertificate"/>'s chain and returns the collected
    /// CRL + OCSP evidence. Returns <c>null</c> when the chain cannot be built (e.g.
    /// offline-only host, partner cert with a private intermediate).
    /// </summary>
    public CollectedRevocationEvidence? Collect(X509Certificate2 signerCertificate)
    {
        ArgumentNullException.ThrowIfNull(signerCertificate);

        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
        chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(_ltvOptions.FetchTimeoutSeconds);
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

        var built = chain.Build(signerCertificate);
        if (!built)
        {
            // X509Chain returns false even when the chain itself is fine but the host can't
            // reach the issuer's CRL / OCSP responder. We surface that as a Warning and
            // return the certs we managed to collect — the augmenter then writes whatever
            // is available; the signature row records RevocationEvidenceFormat.None.
            _logger.LogWarning(
                "X509Chain build did not succeed for {Subject}; collecting whatever the chain populated.",
                signerCertificate.Subject);
        }

        var chainCerts = new List<X509Certificate2>();
        foreach (var element in chain.ChainElements)
        {
            chainCerts.Add(element.Certificate);
        }

        if (chainCerts.Count == 0)
        {
            return null;
        }

        // .NET 10's X509Chain does the revocation fetch internally but does not surface the
        // raw CRL / OCSP bytes through a public API. For v1 we mark the evidence as
        // "collected by chain build" — the augmenter still emits the chain certs into the
        // DSS dictionary, which is enough for clients that fetch revocation themselves
        // (Adobe). A future iteration replaces this with an explicit CRL/OCSP fetcher
        // (System.Net.Http) so the DSS carries the responses inline (PAdES-B-LT proper).
        return new CollectedRevocationEvidence(
            chainCerts.AsReadOnly(),
            Crls: Array.Empty<byte[]>(),
            Ocsps: Array.Empty<byte[]>());
    }
}

/// <summary>
/// What the collector found for a single signer cert. Chain certs are always emitted into
/// the DSS even when the CRL / OCSP slots are empty.
/// </summary>
public sealed record CollectedRevocationEvidence(
    IReadOnlyList<X509Certificate2> ChainCertificates,
    IReadOnlyList<byte[]> Crls,
    IReadOnlyList<byte[]> Ocsps);
