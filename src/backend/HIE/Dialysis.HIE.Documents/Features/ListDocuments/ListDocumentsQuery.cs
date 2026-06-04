using Dialysis.CQRS.Queries;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Documents.Domain;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Documents.Features.ListDocuments;

/// <summary>Row shape returned by the admin documents board.</summary>
public sealed record DocumentRow
{
    /// <summary>Row shape returned by the admin documents board.</summary>
    public DocumentRow(Guid Id,
        Guid PatientId,
        string Kind,
        string Title,
        string MimeType,
        string? LanguageCode,
        DocumentReferenceStatus Status,
        DocumentReferenceSource Source,
        long Size,
        DateTime CreatedAtUtc,
        int SignatureCount,
        bool HasAcroForms,
        bool HasJavascript)
    {
        this.Id = Id;
        this.PatientId = PatientId;
        this.Kind = Kind;
        this.Title = Title;
        this.MimeType = MimeType;
        this.LanguageCode = LanguageCode;
        this.Status = Status;
        this.Source = Source;
        this.Size = Size;
        this.CreatedAtUtc = CreatedAtUtc;
        this.SignatureCount = SignatureCount;
        this.HasAcroForms = HasAcroForms;
        this.HasJavascript = HasJavascript;
    }
    public Guid Id { get; init; }
    public Guid PatientId { get; init; }
    public string Kind { get; init; }
    public string Title { get; init; }
    public string MimeType { get; init; }
    public string? LanguageCode { get; init; }
    public DocumentReferenceStatus Status { get; init; }
    public DocumentReferenceSource Source { get; init; }
    public long Size { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int SignatureCount { get; init; }
    public bool HasAcroForms { get; init; }
    public bool HasJavascript { get; init; }
    public void Deconstruct(out Guid Id, out Guid PatientId, out string Kind, out string Title, out string MimeType, out string? LanguageCode, out DocumentReferenceStatus Status, out DocumentReferenceSource Source, out long Size, out DateTime CreatedAtUtc, out int SignatureCount, out bool HasAcroForms, out bool HasJavascript)
    {
        Id = this.Id;
        PatientId = this.PatientId;
        Kind = this.Kind;
        Title = this.Title;
        MimeType = this.MimeType;
        LanguageCode = this.LanguageCode;
        Status = this.Status;
        Source = this.Source;
        Size = this.Size;
        CreatedAtUtc = this.CreatedAtUtc;
        SignatureCount = this.SignatureCount;
        HasAcroForms = this.HasAcroForms;
        HasJavascript = this.HasJavascript;
    }
}

public sealed record ListDocumentsQuery : IQuery<IReadOnlyList<DocumentRow>>, IPermissionedCommand
{
    public ListDocumentsQuery(Guid? PatientId,
        string? Kind,
        DocumentReferenceStatus? Status,
        DocumentReferenceSource? Source,
        int Take = 50)
    {
        this.PatientId = PatientId;
        this.Kind = Kind;
        this.Status = Status;
        this.Source = Source;
        this.Take = Take;
    }
    public string RequiredPermission => HiePermissions.DocumentsView;
    public Guid? PatientId { get; init; }
    public string? Kind { get; init; }
    public DocumentReferenceStatus? Status { get; init; }
    public DocumentReferenceSource? Source { get; init; }
    public int Take { get; init; }
    public void Deconstruct(out Guid? PatientId, out string? Kind, out DocumentReferenceStatus? Status, out DocumentReferenceSource? Source, out int Take)
    {
        PatientId = this.PatientId;
        Kind = this.Kind;
        Status = this.Status;
        Source = this.Source;
        Take = this.Take;
    }
}
