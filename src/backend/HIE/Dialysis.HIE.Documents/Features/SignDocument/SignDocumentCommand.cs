using System.Security.Cryptography;
using Dialysis.BuildingBlocks.Documents.Signing;
using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Documents.Features.SignDocument;

/// <summary>
/// Apply a PAdES digital signature to an existing document. The cert source picks between
/// the configured platform cert, the per-user cert resolved via the Identity claim, and
/// the eIDAS-QES TSP credential (<see cref="PdfSigningCertificateSource.RemoteQes"/>);
/// the result is persisted as a new bytes version and a <c>DocumentReferenceSignature</c>
/// row is appended (signatures stack — multi-sign is supported). The requested
/// <see cref="PadesLevel"/> drives whether a TSA timestamp and DSS revocation evidence
/// are embedded.
/// </summary>
public sealed record SignDocumentCommand : ICommand<Guid>, IPermissionedCommand
{
    /// <summary>
    /// Apply a PAdES digital signature to an existing document. The cert source picks between
    /// the configured platform cert, the per-user cert resolved via the Identity claim, and
    /// the eIDAS-QES TSP credential (<see cref="PdfSigningCertificateSource.RemoteQes"/>);
    /// the result is persisted as a new bytes version and a <c>DocumentReferenceSignature</c>
    /// row is appended (signatures stack — multi-sign is supported). The requested
    /// <see cref="PadesLevel"/> drives whether a TSA timestamp and DSS revocation evidence
    /// are embedded.
    /// </summary>
    public SignDocumentCommand(Guid DocumentId,
        PdfSigningCertificateSource CertificateSource,
        string? UserId,
        string? Reason,
        string? Location,
        string? ContactInfo,
        PadesConformance Level = PadesConformance.B,
        string? TspCredentialId = null)
    {
        this.DocumentId = DocumentId;
        this.CertificateSource = CertificateSource;
        this.UserId = UserId;
        this.Reason = Reason;
        this.Location = Location;
        this.ContactInfo = ContactInfo;
        this.Level = Level;
        this.TspCredentialId = TspCredentialId;
    }
    public string RequiredPermission => HiePermissions.DocumentsSign;
    public Guid DocumentId { get; init; }
    public PdfSigningCertificateSource CertificateSource { get; init; }
    public string? UserId { get; init; }
    public string? Reason { get; init; }
    public string? Location { get; init; }
    public string? ContactInfo { get; init; }
    public PadesConformance Level { get; init; }
    public string? TspCredentialId { get; init; }
    public void Deconstruct(out Guid DocumentId, out PdfSigningCertificateSource CertificateSource, out string? UserId, out string? Reason, out string? Location, out string? ContactInfo, out PadesConformance Level, out string? TspCredentialId)
    {
        DocumentId = this.DocumentId;
        CertificateSource = this.CertificateSource;
        UserId = this.UserId;
        Reason = this.Reason;
        Location = this.Location;
        ContactInfo = this.ContactInfo;
        Level = this.Level;
        TspCredentialId = this.TspCredentialId;
    }
}

public sealed class SignDocumentCommandHandler : ICommandHandler<SignDocumentCommand, Guid>
{
    private readonly IDocumentReferenceRepository _repository;
    private readonly IDocumentBlobStore _blobs;
    private readonly IPdfSigner _signer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    private readonly IRemoteSignatureService? _remoteSignature;
    public SignDocumentCommandHandler(IDocumentReferenceRepository repository,
        IDocumentBlobStore blobs,
        IPdfSigner signer,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        // Only registered when a TSP (Documents:Signing:Tsp:BaseUri) is configured. The DI
        // container treats a parameter as optional only when it has a default value — a nullable
        // annotation alone is not enough — so keep the `= null` default and trailing position.
        IRemoteSignatureService? remoteSignature = null)
    {
        _repository = repository;
        _blobs = blobs;
        _signer = signer;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _remoteSignature = remoteSignature;
    }
    public async Task<Guid> HandleAsync(SignDocumentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var document = await _repository.FindAsync(request.DocumentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Document {request.DocumentId} not found.");
        if (!string.Equals(document.MimeType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Only PDF documents can be digitally signed.");

        var bytes = await _blobs.ReadAsync(document.StorageRef, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Blob {document.StorageRef} is missing.");

        var signingRequest = new PdfSigningRequest(
            request.CertificateSource,
            request.UserId,
            request.Reason,
            request.Location,
            request.ContactInfo)
        {
            Level = request.Level,
            TspCredentialId = request.TspCredentialId,
        };

        var result = await _signer.SignAsync(bytes, signingRequest, cancellationToken).ConfigureAwait(false);

        var newRef = await _blobs.SaveAsync(Guid.CreateVersion7(), document.MimeType, result.SignedPdf, cancellationToken)
            .ConfigureAwait(false);
        var newHash = Convert.ToHexString(SHA256.HashData(result.SignedPdf));
        document.Revise(newRef, newHash, result.SignedPdf.LongLength, document.HasAcroForms, document.HasJavascript);

        var signerKind = request.CertificateSource switch
        {
            PdfSigningCertificateSource.Platform => DocumentSignerKind.Platform,
            PdfSigningCertificateSource.User => DocumentSignerKind.User,
            PdfSigningCertificateSource.RemoteQes => DocumentSignerKind.RemoteQes,
            _ => throw new InvalidOperationException($"Unsupported signing source '{request.CertificateSource}'."),
        };

        var padesLevel = MapLevel(result.Level);
        var revocationFormat = MapRevocationFormat(result.Revocation?.Kind);
        var signatureFormat = result.IsQualified ? SignatureFormat.Qes : SignatureFormat.Aes;

        document.RecordSignature(new DocumentReferenceSignature(
            id: Guid.CreateVersion7(),
            documentReferenceId: document.Id,
            signerKind: signerKind,
            certThumbprint: result.CertThumbprint,
            signedAtUtc: _clock.GetUtcNow().UtcDateTime,
            padesLevel: padesLevel,
            signatureFormat: signatureFormat,
            signerUserId: request.UserId,
            reason: request.Reason,
            tsaUri: result.TsaUri,
            tsaCertThumbprint: result.TsaCertThumbprint,
            timestampedAtUtc: result.TimestampedAtUtc,
            revocationEvidenceFormat: revocationFormat,
            revocationEvidenceBlob: result.Revocation?.Blob,
            tspId: result.IsQualified ? _remoteSignature?.TspId : null,
            tspCredentialId: request.TspCredentialId));

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return document.Id;
    }

    private static PadesLevel MapLevel(PadesConformance level) => level switch
    {
        PadesConformance.T => PadesLevel.T,
        PadesConformance.Lt => PadesLevel.Lt,
        PadesConformance.Lta => PadesLevel.Lta,
        _ => PadesLevel.B,
    };

    private static RevocationEvidenceFormat MapRevocationFormat(RevocationEvidenceKind? kind) => kind switch
    {
        RevocationEvidenceKind.Crl => RevocationEvidenceFormat.Crl,
        RevocationEvidenceKind.Ocsp => RevocationEvidenceFormat.Ocsp,
        RevocationEvidenceKind.Both => RevocationEvidenceFormat.Both,
        _ => RevocationEvidenceFormat.None,
    };
}
