using System.Security.Cryptography.X509Certificates;

namespace Dialysis.BuildingBlocks.Documents.Signing;

/// <summary>
/// Embeds a PAdES-style PKCS#7 signature into an existing PDF. The signer must preserve
/// the original document byte-for-byte — including any embedded JavaScript actions
/// (<c>/JS</c>, <c>/OpenAction</c>, <c>/AA</c>) and AcroForm widget tree — so the
/// signature applies to the same content a reviewer sees in the viewer.
/// </summary>
public interface IPdfSigner
{
    /// <summary>Returns the signed PDF bytes.</summary>
    Task<byte[]> SignAsync(ReadOnlyMemory<byte> pdf, PdfSigningRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Caller-supplied signing parameters. Identifies which cert to use (resolved at sign time
/// via <see cref="ISigningCertificateResolver"/>) and the human-readable signature metadata
/// embedded in the signature dictionary.
/// </summary>
public sealed record PdfSigningRequest(
    PdfSigningCertificateSource CertificateSource,
    string? UserId,
    string? Reason,
    string? Location,
    string? ContactInfo)
{
    /// <summary>Optional visible-signature placement; when <c>null</c> the signature is invisible.</summary>
    public VisibleSignaturePlacement? VisiblePlacement { get; init; }
}

/// <summary>Where the signing certificate comes from at sign time.</summary>
public enum PdfSigningCertificateSource
{
    /// <summary>Single platform / SMC-B organisational cert configured on the host.</summary>
    Platform = 1,

    /// <summary>Per-user cert looked up via the user's Identity claim (<see cref="PdfSigningRequest.UserId"/>).</summary>
    User = 2,
}

/// <summary>Visible-signature placement (PDF user-space points, origin bottom-left).</summary>
public sealed record VisibleSignaturePlacement(int PageNumber, double X, double Y, double Width, double Height);

/// <summary>
/// Resolves the X509 certificate (with private key) used to sign a document. Two
/// implementations ship out of the box: <c>ConfiguredPlatformCertificateResolver</c>
/// and <c>KeycloakUserCertificateResolver</c>. Hosts can register additional resolvers
/// (e.g. a future eIDAS-qualified TSP-backed resolver) without touching <see cref="IPdfSigner"/>.
/// </summary>
public interface ISigningCertificateResolver
{
    /// <summary>Which sources this resolver answers for; the dispatcher picks the matching resolver at sign time.</summary>
    PdfSigningCertificateSource Source { get; }

    /// <summary>
    /// Returns the cert (with private key) to use, or throws if the request can't be satisfied
    /// (e.g. the user has no cert provisioned).
    /// </summary>
    Task<X509Certificate2> ResolveAsync(PdfSigningRequest request, CancellationToken cancellationToken);
}
