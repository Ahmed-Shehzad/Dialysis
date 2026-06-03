using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Documents.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Documents.Features.DeleteDocument;

/// <summary>Soft-delete (status → EnteredInError). Blob bytes are retained so a future audit-replay can read them.</summary>
public sealed record DeleteDocumentCommand(Guid DocumentId) : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.DocumentsDelete;
}

public sealed class DeleteDocumentCommandHandler(
    IDocumentReferenceRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteDocumentCommand>
{
    public async Task<Unit> HandleAsync(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await repository.FindAsync(request.DocumentId, cancellationToken).ConfigureAwait(false);
        if (document is null) return Unit.Value;
        document.EnterInError();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
