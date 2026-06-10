using Dialysis.CQRS.Queries;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Documents.Features.GetDocument;

/// <summary>Detail view including the signature history.</summary>
public sealed record DocumentDetail
{
    /// <summary>Detail view including the signature history.</summary>
    public DocumentDetail(Guid Id,
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
        bool AllowJavaScriptExecution,
        IReadOnlyList<DocumentSignatureRow> Signatures)
    {
        this.Id = Id;
        this.PatientId = PatientId;
        this.Kind = Kind;
        this.Category = Category;
        this.Title = Title;
        this.MimeType = MimeType;
        this.LanguageCode = LanguageCode;
        this.Status = Status;
        this.Source = Source;
        this.Size = Size;
        this.CreatedAtUtc = CreatedAtUtc;
        this.CreatedBy = CreatedBy;
        this.ContentHash = ContentHash;
        this.HasAcroForms = HasAcroForms;
        this.HasJavascript = HasJavascript;
        this.AllowJavaScriptExecution = AllowJavaScriptExecution;
        this.Signatures = Signatures;
    }
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string Kind { get; init; }
    public string? Category { get; init; }
    public string Title { get; init; }
    public string MimeType { get; init; }
    public string? LanguageCode { get; init; }
    public DocumentReferenceStatus Status { get; init; }
    public DocumentReferenceSource Source { get; init; }
    public long Size { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public string? CreatedBy { get; init; }
    public string ContentHash { get; init; }
    public bool HasAcroForms { get; init; }
    public bool HasJavascript { get; init; }
    public bool AllowJavaScriptExecution { get; init; }
    public IReadOnlyList<DocumentSignatureRow> Signatures { get; init; }
    public void Deconstruct(out Guid Id, out Guid PatientId, out string Kind, out string? Category, out string Title, out string MimeType, out string? LanguageCode, out DocumentReferenceStatus Status, out DocumentReferenceSource Source, out long Size, out DateTime CreatedAtUtc, out string? CreatedBy, out string ContentHash, out bool HasAcroForms, out bool HasJavascript, out bool AllowJavaScriptExecution, out IReadOnlyList<DocumentSignatureRow> Signatures)
    {
        Id = this.Id;
        PatientId = this.PatientId;
        Kind = this.Kind;
        Category = this.Category;
        Title = this.Title;
        MimeType = this.MimeType;
        LanguageCode = this.LanguageCode;
        Status = this.Status;
        Source = this.Source;
        Size = this.Size;
        CreatedAtUtc = this.CreatedAtUtc;
        CreatedBy = this.CreatedBy;
        ContentHash = this.ContentHash;
        HasAcroForms = this.HasAcroForms;
        HasJavascript = this.HasJavascript;
        AllowJavaScriptExecution = this.AllowJavaScriptExecution;
        Signatures = this.Signatures;
    }
}

public sealed record DocumentSignatureRow
{
    public DocumentSignatureRow(Guid Id,
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
        string? TspCredentialId)
    {
        this.Id = Id;
        this.SignerKind = SignerKind;
        this.SignerUserId = SignerUserId;
        this.CertThumbprint = CertThumbprint;
        this.SignedAtUtc = SignedAtUtc;
        this.Reason = Reason;
        this.PadesLevel = PadesLevel;
        this.SignatureFormat = SignatureFormat;
        this.TsaUri = TsaUri;
        this.TimestampedAtUtc = TimestampedAtUtc;
        this.RevocationEvidenceFormat = RevocationEvidenceFormat;
        this.TspId = TspId;
        this.TspCredentialId = TspCredentialId;
    }
    public Guid Id { get; init; }
    public DocumentSignerKind SignerKind { get; init; }
    public string? SignerUserId { get; init; }
    public string CertThumbprint { get; init; }
    public DateTime SignedAtUtc { get; init; }
    public string? Reason { get; init; }
    public PadesLevel PadesLevel { get; init; }
    public SignatureFormat SignatureFormat { get; init; }
    public string? TsaUri { get; init; }
    public DateTime? TimestampedAtUtc { get; init; }
    public RevocationEvidenceFormat RevocationEvidenceFormat { get; init; }
    public string? TspId { get; init; }
    public string? TspCredentialId { get; init; }
    public void Deconstruct(out Guid Id, out DocumentSignerKind SignerKind, out string? SignerUserId, out string CertThumbprint, out DateTime SignedAtUtc, out string? Reason, out PadesLevel PadesLevel, out SignatureFormat SignatureFormat, out string? TsaUri, out DateTime? TimestampedAtUtc, out RevocationEvidenceFormat RevocationEvidenceFormat, out string? TspId, out string? TspCredentialId)
    {
        Id = this.Id;
        SignerKind = this.SignerKind;
        SignerUserId = this.SignerUserId;
        CertThumbprint = this.CertThumbprint;
        SignedAtUtc = this.SignedAtUtc;
        Reason = this.Reason;
        PadesLevel = this.PadesLevel;
        SignatureFormat = this.SignatureFormat;
        TsaUri = this.TsaUri;
        TimestampedAtUtc = this.TimestampedAtUtc;
        RevocationEvidenceFormat = this.RevocationEvidenceFormat;
        TspId = this.TspId;
        TspCredentialId = this.TspCredentialId;
    }
}

public sealed record GetDocumentQuery : IQuery<DocumentDetail?>, IPermissionedCommand
{
    public GetDocumentQuery(Guid Id) => this.Id = Id;
    public string RequiredPermission => HiePermissions.DocumentsView;
    public Guid Id { get; init; }
    public void Deconstruct(out Guid Id) => Id = this.Id;
}

public sealed class GetDocumentQueryHandler : IQueryHandler<GetDocumentQuery, DocumentDetail?>
{
    private readonly IDocumentReferenceRepository _repository;
    public GetDocumentQueryHandler(IDocumentReferenceRepository repository) => _repository = repository;
    public async Task<DocumentDetail?> HandleAsync(GetDocumentQuery request, CancellationToken cancellationToken)
    {
        var doc = await _repository.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (doc is null)
            return null;
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
            doc.AllowJavaScriptExecution,
            [.. doc.Signatures.Select(s => new DocumentSignatureRow(
                s.Id, s.SignerKind, s.SignerUserId, s.CertThumbprint, s.SignedAtUtc, s.Reason,
                s.PadesLevel, s.SignatureFormat, s.TsaUri, s.TimestampedAtUtc,
                s.RevocationEvidenceFormat, s.TspId, s.TspCredentialId))]);
    }
}
