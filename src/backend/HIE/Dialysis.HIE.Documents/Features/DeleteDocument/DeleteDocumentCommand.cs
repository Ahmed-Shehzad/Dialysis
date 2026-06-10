using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Documents.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Documents.Features.DeleteDocument;

/// <summary>Soft-delete (status → EnteredInError). Blob bytes are retained so a future audit-replay can read them.</summary>
public sealed record DeleteDocumentCommand : ICommand, IPermissionedCommand
{
    /// <summary>Soft-delete (status → EnteredInError). Blob bytes are retained so a future audit-replay can read them.</summary>
    public DeleteDocumentCommand(Guid DocumentId) => this.DocumentId = DocumentId;
    public string RequiredPermission => HiePermissions.DocumentsDelete;
    public Guid DocumentId { get; init; }
    public void Deconstruct(out Guid DocumentId) => DocumentId = this.DocumentId;
}

public sealed class DeleteDocumentCommandHandler : ICommandHandler<DeleteDocumentCommand>
{
    private readonly IDocumentReferenceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteDocumentCommandHandler(IDocumentReferenceRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
    public async Task<Unit> HandleAsync(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _repository.FindAsync(request.DocumentId, cancellationToken).ConfigureAwait(false);
        if (document is null)
            return Unit.Value;
        document.EnterInError();
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
