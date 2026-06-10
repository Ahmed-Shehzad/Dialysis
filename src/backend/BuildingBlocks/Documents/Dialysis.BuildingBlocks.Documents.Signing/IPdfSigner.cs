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
public sealed record PdfSigningRequest
{
    /// <summary>
    /// Caller-supplied signing parameters. Identifies which cert to use (resolved at sign time
    /// via <see cref="ISigningCertificateResolver"/>) and the human-readable signature metadata
    /// embedded in the signature dictionary.
    /// </summary>
    public PdfSigningRequest(PdfSigningCertificateSource CertificateSource,
        string? UserId,
        string? Reason,
        string? Location,
        string? ContactInfo)
    {
        this.CertificateSource = CertificateSource;
        this.UserId = UserId;
        this.Reason = Reason;
        this.Location = Location;
        this.ContactInfo = ContactInfo;
    }

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

    public PdfSigningCertificateSource CertificateSource { get; init; }
    public string? UserId { get; init; }
    public string? Reason { get; init; }
    public string? Location { get; init; }
    public string? ContactInfo { get; init; }
    public void Deconstruct(out PdfSigningCertificateSource certificateSource, out string? userId, out string? reason, out string? location, out string? contactInfo)
    {
        certificateSource = CertificateSource;
        userId = UserId;
        reason = Reason;
        location = Location;
        contactInfo = ContactInfo;
    }
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
    Lt = 3,

    /// <summary>PAdES-B-LTA — LT plus a document-timestamp over the DSS.</summary>
    Lta = 4,
}

/// <summary>Visible-signature placement (PDF user-space points, origin bottom-left).</summary>
public sealed record VisibleSignaturePlacement
{
    /// <summary>Visible-signature placement (PDF user-space points, origin bottom-left).</summary>
    public VisibleSignaturePlacement(int PageNumber, double X, double Y, double Width, double Height)
    {
        this.PageNumber = PageNumber;
        this.X = X;
        this.Y = Y;
        this.Width = Width;
        this.Height = Height;
    }
    public int PageNumber { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
    public void Deconstruct(out int pageNumber, out double x, out double y, out double width, out double height)
    {
        pageNumber = PageNumber;
        x = X;
        y = Y;
        width = Width;
        height = Height;
    }
}

/// <summary>
/// Result of a successful sign operation. Carries the signed PDF bytes together with the
/// audit-trail metadata the caller persists on its signature row.
/// </summary>
public sealed record PdfSigningResult
{
    /// <summary>
    /// Result of a successful sign operation. Carries the signed PDF bytes together with the
    /// audit-trail metadata the caller persists on its signature row.
    /// </summary>
    public PdfSigningResult(byte[] SignedPdf,
        string CertThumbprint,
        PadesConformance Level,
        bool IsQualified,
        string? TsaUri,
        string? TsaCertThumbprint,
        DateTime? TimestampedAtUtc,
        RevocationEvidence? Revocation)
    {
        this.SignedPdf = SignedPdf;
        this.CertThumbprint = CertThumbprint;
        this.Level = Level;
        this.IsQualified = IsQualified;
        this.TsaUri = TsaUri;
        this.TsaCertThumbprint = TsaCertThumbprint;
        this.TimestampedAtUtc = TimestampedAtUtc;
        this.Revocation = Revocation;
    }
    public byte[] SignedPdf { get; init; }
    public string CertThumbprint { get; init; }
    public PadesConformance Level { get; init; }
    public bool IsQualified { get; init; }
    public string? TsaUri { get; init; }
    public string? TsaCertThumbprint { get; init; }
    public DateTime? TimestampedAtUtc { get; init; }
    public RevocationEvidence? Revocation { get; init; }
    public void Deconstruct(out byte[] signedPdf, out string certThumbprint, out PadesConformance level, out bool isQualified, out string? tsaUri, out string? tsaCertThumbprint, out DateTime? timestampedAtUtc, out RevocationEvidence? revocation)
    {
        signedPdf = SignedPdf;
        certThumbprint = CertThumbprint;
        level = Level;
        isQualified = IsQualified;
        tsaUri = TsaUri;
        tsaCertThumbprint = TsaCertThumbprint;
        timestampedAtUtc = TimestampedAtUtc;
        revocation = Revocation;
    }
}

/// <summary>Revocation evidence embedded in the signed PDF's DSS dictionary.</summary>
public sealed record RevocationEvidence
{
    /// <summary>Revocation evidence embedded in the signed PDF's DSS dictionary.</summary>
    public RevocationEvidence(RevocationEvidenceKind Kind, byte[] Blob)
    {
        this.Kind = Kind;
        this.Blob = Blob;
    }
    public RevocationEvidenceKind Kind { get; init; }
    public byte[] Blob { get; init; }
    public void Deconstruct(out RevocationEvidenceKind kind, out byte[] blob)
    {
        kind = Kind;
        blob = Blob;
    }
}

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
