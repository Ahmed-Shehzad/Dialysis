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

public sealed record ParsedTrustAnchor
{
    public ParsedTrustAnchor(string Subject,
        string Thumbprint,
        DateTime NotBefore,
        DateTime NotAfter,
        string CertificatePem)
    {
        this.Subject = Subject;
        this.Thumbprint = Thumbprint;
        this.NotBefore = NotBefore;
        this.NotAfter = NotAfter;
        this.CertificatePem = CertificatePem;
    }
    public string Subject { get; init; }
    public string Thumbprint { get; init; }
    public DateTime NotBefore { get; init; }
    public DateTime NotAfter { get; init; }
    public string CertificatePem { get; init; }
    public void Deconstruct(out string Subject, out string Thumbprint, out DateTime NotBefore, out DateTime NotAfter, out string CertificatePem)
    {
        Subject = this.Subject;
        Thumbprint = this.Thumbprint;
        NotBefore = this.NotBefore;
        NotAfter = this.NotAfter;
        CertificatePem = this.CertificatePem;
    }
}
