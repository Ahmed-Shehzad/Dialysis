using Dialysis.CQRS.Queries;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Documents.Features.GetDocument;

/// <summary>Detail view including the signature history.</summary>
public sealed record DocumentDetail(
    Guid Id,
    Guid PatientId,
    string Kind,
    string? Category,
    string Title,
    string MimeType,
    string? LanguageCode,
    DocumentReferenceStatus Status,
    DocumentReferenceSource Source,
    long Size,
    DateTime CreatedAtUtc,
    string? CreatedBy,
    string ContentHash,
    bool HasAcroForms,
    bool HasJavascript,
    IReadOnlyList<DocumentSignatureRow> Signatures);

public sealed record DocumentSignatureRow(
    Guid Id,
    DocumentSignerKind SignerKind,
    string? SignerUserId,
    string CertThumbprint,
    DateTime SignedAtUtc,
    string? Reason,
    PadesLevel PadesLevel,
    SignatureFormat SignatureFormat,
    string? TsaUri,
    DateTime? TimestampedAtUtc,
    RevocationEvidenceFormat RevocationEvidenceFormat,
    string? TspId,
    string? TspCredentialId);

public sealed record GetDocumentQuery(Guid Id) : IQuery<DocumentDetail?>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.DocumentsView;
}

public sealed class GetDocumentQueryHandler(IDocumentReferenceRepository repository)
    : IQueryHandler<GetDocumentQuery, DocumentDetail?>
{
    public async Task<DocumentDetail?> HandleAsync(GetDocumentQuery request, CancellationToken cancellationToken)
    {
        var doc = await repository.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (doc is null) return null;
        return new DocumentDetail(
            doc.Id,
            doc.PatientId,
            doc.Kind,
            doc.Category,
            doc.Title,
            doc.MimeType,
            doc.LanguageCode,
            doc.Status,
            doc.Source,
            doc.Size,
            doc.CreatedAtUtc,
            doc.CreatedBy,
            doc.ContentHash,
            doc.HasAcroForms,
            doc.HasJavascript,
            [.. doc.Signatures.Select(s => new DocumentSignatureRow(
                s.Id, s.SignerKind, s.SignerUserId, s.CertThumbprint, s.SignedAtUtc, s.Reason,
                s.PadesLevel, s.SignatureFormat, s.TsaUri, s.TimestampedAtUtc,
                s.RevocationEvidenceFormat, s.TspId, s.TspCredentialId))]);
    }
}
