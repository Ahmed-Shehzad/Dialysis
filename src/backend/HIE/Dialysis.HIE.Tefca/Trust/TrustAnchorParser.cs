using System.Security.Cryptography.X509Certificates;

namespace Dialysis.HIE.Tefca.Trust;

/// <summary>
/// Parses a PEM-encoded X.509 certificate into the metadata fields the
/// <c>QhinTrustAnchor</c> aggregate stores (subject, thumbprint, validity window) while
/// preserving the original PEM bytes for downstream chain validation. Centralised so a
/// future hardening pass (CRL/OCSP attachment, key-usage validation) lands in one place.
/// </summary>
public static class TrustAnchorParser
{
    public static ParsedTrustAnchor Parse(string certificatePem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePem);
        try
        {
            using var certificate = X509Certificate2.CreateFromPem(certificatePem);
            return new ParsedTrustAnchor(
                Subject: certificate.Subject,
                Thumbprint: certificate.Thumbprint,
                NotBefore: certificate.NotBefore.ToUniversalTime(),
                NotAfter: certificate.NotAfter.ToUniversalTime(),
                CertificatePem: certificatePem);
        }
        catch (Exception ex) when (ex is FormatException or System.Security.Cryptography.CryptographicException)
        {
            throw new ArgumentException(
                "Trust anchor PEM is not a valid X.509 certificate.", nameof(certificatePem), ex);
        }
    }
}

public sealed record ParsedTrustAnchor(
    string Subject,
    string Thumbprint,
    DateTime NotBefore,
    DateTime NotAfter,
    string CertificatePem);
