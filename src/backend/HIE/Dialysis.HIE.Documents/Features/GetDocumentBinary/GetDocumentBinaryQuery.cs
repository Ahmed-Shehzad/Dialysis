using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.CQRS.Queries;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Documents.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Documents.Features.GetDocumentBinary;

/// <summary>Raw bytes + MIME for a document. The controller streams the bytes back.</summary>
public sealed record DocumentBinary(string MimeType, byte[] Bytes);

public sealed record GetDocumentBinaryQuery(Guid Id) : IQuery<DocumentBinary?>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.DocumentsView;
}

public sealed class GetDocumentBinaryQueryHandler(
    IDocumentReferenceRepository repository,
    IDocumentBlobStore blobs)
    : IQueryHandler<GetDocumentBinaryQuery, DocumentBinary?>
{
    public async Task<DocumentBinary?> HandleAsync(GetDocumentBinaryQuery request, CancellationToken cancellationToken)
    {
        var document = await repository.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (document is null) return null;
        var bytes = await blobs.ReadAsync(document.StorageRef, cancellationToken).ConfigureAwait(false);
        if (bytes is null) return null;
        return new DocumentBinary(document.MimeType, bytes);
    }
}
