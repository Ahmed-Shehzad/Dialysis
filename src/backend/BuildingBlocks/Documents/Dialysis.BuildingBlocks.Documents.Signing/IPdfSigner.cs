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
    /// <summary>Returns the signed PDF bytes together with metadata captured during the sign operation.</summary>
    Task<PdfSigningResult> SignAsync(ReadOnlyMemory<byte> pdf, PdfSigningRequest request, CancellationToken cancellationToken);
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

    /// <summary>
    /// PAdES conformance level to produce. <see cref="PadesConformance.B"/> is the legacy
    /// PR #128 path; higher levels require a configured TSA + revocation evidence.
    /// </summary>
    public PadesConformance Level { get; init; } = PadesConformance.B;

    /// <summary>
    /// TSP credential identifier — only used when <see cref="CertificateSource"/> is
    /// <see cref="PdfSigningCertificateSource.RemoteQes"/>. Identifies which TSP-held
    /// credential to sign with (CSC v2 <c>credentialID</c>).
    /// </summary>
    public string? TspCredentialId { get; init; }
}

/// <summary>Where the signing certificate comes from at sign time.</summary>
public enum PdfSigningCertificateSource
{
    /// <summary>Single platform / SMC-B organisational cert configured on the host.</summary>
    Platform = 1,

    /// <summary>Per-user cert looked up via the user's Identity claim (<see cref="PdfSigningRequest.UserId"/>).</summary>
    User = 2,

    /// <summary>Qualified-signature credential held by an eIDAS TSP, signed via the CSC v2 protocol.</summary>
    RemoteQes = 3,
}

/// <summary>
/// PAdES conformance level the signer should produce. Mirrors the ETSI EN 319 142-1 levels;
/// HIE persists the value on the <c>DocumentReferenceSignature</c> row.
/// </summary>
public enum PadesConformance
{
    /// <summary>PAdES-B-B — baseline, no TSA, no DSS. Pre-#128 default.</summary>
    B = 1,

    /// <summary>PAdES-B-T — TSA-stamped signing time embedded.</summary>
    T = 2,

    /// <summary>PAdES-B-LT — TSA + DSS revocation evidence packed in.</summary>
    LT = 3,

    /// <summary>PAdES-B-LTA — LT plus a document-timestamp over the DSS.</summary>
    LTA = 4,
}

/// <summary>Visible-signature placement (PDF user-space points, origin bottom-left).</summary>
public sealed record VisibleSignaturePlacement(int PageNumber, double X, double Y, double Width, double Height);

/// <summary>
/// Result of a successful sign operation. Carries the signed PDF bytes together with the
/// audit-trail metadata the caller persists on its signature row.
/// </summary>
public sealed record PdfSigningResult(
    byte[] SignedPdf,
    string CertThumbprint,
    PadesConformance Level,
    bool IsQualified,
    string? TsaUri,
    string? TsaCertThumbprint,
    DateTime? TimestampedAtUtc,
    RevocationEvidence? Revocation);

/// <summary>Revocation evidence embedded in the signed PDF's DSS dictionary.</summary>
public sealed record RevocationEvidence(RevocationEvidenceKind Kind, byte[] Blob);

/// <summary>Which revocation primitives the DSS dictionary carries.</summary>
public enum RevocationEvidenceKind
{
    None = 0,
    Crl = 1,
    Ocsp = 2,
    Both = 3,
}

/// <summary>
/// Resolves the X509 certificate used to sign a document. Platform / User resolvers return
/// a cert <em>with</em> its private key (local PKCS#7 signing); the QES resolver returns
/// the public cert only and the signer delegates the hash-signing step to
/// <see cref="IRemoteSignatureService"/>.
/// </summary>
public interface ISigningCertificateResolver
{
    /// <summary>Which sources this resolver answers for; the dispatcher picks the matching resolver at sign time.</summary>
    PdfSigningCertificateSource Source { get; }

    /// <summary>
    /// Returns the cert to use, or throws if the request can't be satisfied (e.g. the user
    /// has no cert provisioned, or the TSP rejected the credential id).
    /// </summary>
    Task<X509Certificate2> ResolveAsync(PdfSigningRequest request, CancellationToken cancellationToken);
}
