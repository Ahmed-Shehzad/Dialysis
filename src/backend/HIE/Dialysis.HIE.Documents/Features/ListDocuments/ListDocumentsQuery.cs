using Dialysis.CQRS.Queries;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Documents.Domain;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Documents.Features.ListDocuments;

/// <summary>Row shape returned by the admin documents board.</summary>
public sealed record DocumentRow(
    Guid Id,
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
    bool HasJavascript);

public sealed record ListDocumentsQuery(
    Guid? PatientId,
    string? Kind,
    DocumentReferenceStatus? Status,
    DocumentReferenceSource? Source,
    int Take = 50)
    : IQuery<IReadOnlyList<DocumentRow>>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.DocumentsView;
}
