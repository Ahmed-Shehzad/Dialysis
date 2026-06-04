using System.Security.Cryptography;
using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Documents.Features.UploadDocument;

/// <summary>
/// Operator-driven admin upload of a clinical document. Payload is base64-encoded so the
/// front end can stay on the existing application/json content-type (mirrors the file
/// upload pattern already used by <c>NewChannelDialog</c>).
/// </summary>
public sealed record UploadDocumentCommand : ICommand<Guid>, IPermissionedCommand
{
    /// <summary>
    /// Operator-driven admin upload of a clinical document. Payload is base64-encoded so the
    /// front end can stay on the existing application/json content-type (mirrors the file
    /// upload pattern already used by <c>NewChannelDialog</c>).
    /// </summary>
    public UploadDocumentCommand(Guid PatientId,
        string Kind,
        string Title,
        string MimeType,
        string Base64Content,
        string? LanguageCode,
        string? Category,
        string? CreatedBy)
    {
        this.PatientId = PatientId;
        this.Kind = Kind;
        this.Title = Title;
        this.MimeType = MimeType;
        this.Base64Content = Base64Content;
        this.LanguageCode = LanguageCode;
        this.Category = Category;
        this.CreatedBy = CreatedBy;
    }
    public string RequiredPermission => HiePermissions.DocumentsUpload;
    public Guid PatientId { get; init; }
    public string Kind { get; init; }
    public string Title { get; init; }
    public string MimeType { get; init; }
    public string Base64Content { get; init; }
    public string? LanguageCode { get; init; }
    public string? Category { get; init; }
    public string? CreatedBy { get; init; }
    public void Deconstruct(out Guid PatientId, out string Kind, out string Title, out string MimeType, out string Base64Content, out string? LanguageCode, out string? Category, out string? CreatedBy)
    {
        PatientId = this.PatientId;
        Kind = this.Kind;
        Title = this.Title;
        MimeType = this.MimeType;
        Base64Content = this.Base64Content;
        LanguageCode = this.LanguageCode;
        Category = this.Category;
        CreatedBy = this.CreatedBy;
    }
}

public sealed class UploadDocumentCommandHandler : ICommandHandler<UploadDocumentCommand, Guid>
{
    private readonly IDocumentReferenceRepository _repository;
    private readonly IDocumentBlobStore _blobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    public UploadDocumentCommandHandler(IDocumentReferenceRepository repository,
        IDocumentBlobStore blobs,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _repository = repository;
        _blobs = blobs;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }
    public async Task<Guid> HandleAsync(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Base64Content);

        var bytes = Convert.FromBase64String(request.Base64Content);
        var documentId = Guid.CreateVersion7();
        var storageRef = await _blobs.SaveAsync(documentId, request.MimeType, bytes, cancellationToken).ConfigureAwait(false);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));

        var pdfFlags = DetectPdfFlags(request.MimeType, bytes);

        var document = new DocumentReference(
            id: documentId,
            patientId: request.PatientId,
            kind: request.Kind,
            title: request.Title,
            mimeType: request.MimeType,
            storageRef: storageRef,
            contentHash: hash,
            size: bytes.LongLength,
            source: DocumentReferenceSource.AdminUpload,
            createdAtUtc: _clock.GetUtcNow().UtcDateTime,
            createdBy: request.CreatedBy,
            category: request.Category,
            languageCode: request.LanguageCode,
            hasAcroForms: pdfFlags.acroForms,
            hasJavascript: pdfFlags.javascript);

        _repository.Add(document);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return document.Id;
    }

    /// <summary>
    /// Lightweight PDF feature probe — checks for AcroForm + JavaScript markers in the raw
    /// bytes. Substring scanning is deliberate: full PDF parsing belongs in the viewer (which
    /// has the user-context to execute scripts safely), the index just needs to surface
    /// "this document has interactive bits" so the operator knows to expect prompts.
    /// </summary>
    private static (bool acroForms, bool javascript) DetectPdfFlags(string mimeType, byte[] bytes)
    {
        if (!string.Equals(mimeType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return (false, false);
        var span = bytes.AsSpan();
        bool acro = ContainsAscii(span, "/AcroForm"u8);
        bool js = ContainsAscii(span, "/JS"u8) || ContainsAscii(span, "/JavaScript"u8) || ContainsAscii(span, "/OpenAction"u8) || ContainsAscii(span, "/AA"u8);
        return (acro, js);
    }

    private static bool ContainsAscii(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle) =>
        haystack.IndexOf(needle) >= 0;
}
