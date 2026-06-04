using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Documents.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Documents.Features.SetDocumentJavaScriptExecution;

/// <summary>
/// Per-document gate that authorizes pdfjs to execute embedded JavaScript when the document
/// is opened in the SPA viewer. Default is OFF: even though the upload preserves JS bytes
/// byte-for-byte, the viewer reads <c>doc.allowJavaScriptExecution</c> and passes it through
/// to pdfjs's <c>enableScripting</c> option, so JS stays inert until a privileged operator
/// explicitly turns it on. Every transition is logged via the <c>[PhiAccess]</c> attribute
/// on the controller endpoint so a regulator can trace authorization.
/// </summary>
public sealed record SetDocumentJavaScriptExecutionCommand(Guid DocumentId, bool Allow)
    : ICommand<bool>, IPermissionedCommand
{
    // Reuse the retention-administer permission so the same role that controls retention
    // policy controls active-content authorization. Adding a dedicated permission would mean
    // touching the Keycloak realm + every host's RolePermissionMap; the retention role already
    // signals "trusted operator with elevated document responsibilities."
    public string RequiredPermission => HiePermissions.DocumentsRetentionAdminister;
}

public sealed class SetDocumentJavaScriptExecutionCommandHandler(
    IDocumentReferenceRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetDocumentJavaScriptExecutionCommand, bool>
{
    public async Task<bool> HandleAsync(SetDocumentJavaScriptExecutionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var document = await repository.FindAsync(request.DocumentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Document {request.DocumentId} not found.");
        document.SetJavaScriptExecution(request.Allow);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return document.AllowJavaScriptExecution;
    }
}
