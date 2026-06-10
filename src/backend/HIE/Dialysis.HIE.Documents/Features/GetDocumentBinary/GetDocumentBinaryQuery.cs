using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.CQRS.Queries;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Documents.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Documents.Features.GetDocumentBinary;

/// <summary>Raw bytes + MIME for a document. The controller streams the bytes back.</summary>
public sealed record DocumentBinary
{
    /// <summary>Raw bytes + MIME for a document. The controller streams the bytes back.</summary>
    public DocumentBinary(string MimeType, byte[] Bytes)
    {
        this.MimeType = MimeType;
        this.Bytes = Bytes;
    }
    public string MimeType { get; init; }
    public byte[] Bytes { get; init; }
    public void Deconstruct(out string MimeType, out byte[] Bytes)
    {
        MimeType = this.MimeType;
        Bytes = this.Bytes;
    }
}

public sealed record GetDocumentBinaryQuery : IQuery<DocumentBinary?>, IPermissionedCommand
{
    public GetDocumentBinaryQuery(Guid Id) => this.Id = Id;
    public string RequiredPermission => HiePermissions.DocumentsView;
    public Guid Id { get; init; }
    public void Deconstruct(out Guid Id) => Id = this.Id;
}

public sealed class GetDocumentBinaryQueryHandler : IQueryHandler<GetDocumentBinaryQuery, DocumentBinary?>
{
    private readonly IDocumentReferenceRepository _repository;
    private readonly IDocumentBlobStore _blobs;
    public GetDocumentBinaryQueryHandler(IDocumentReferenceRepository repository,
        IDocumentBlobStore blobs)
    {
        _repository = repository;
        _blobs = blobs;
    }
    public async Task<DocumentBinary?> HandleAsync(GetDocumentBinaryQuery request, CancellationToken cancellationToken)
    {
        var document = await _repository.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (document is null)
            return null;
        var bytes = await _blobs.ReadAsync(document.StorageRef, cancellationToken).ConfigureAwait(false);
        if (bytes is null)
            return null;
        return new DocumentBinary(document.MimeType, bytes);
    }
}
