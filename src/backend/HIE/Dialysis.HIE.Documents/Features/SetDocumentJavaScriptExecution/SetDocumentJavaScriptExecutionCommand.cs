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
public sealed record SetDocumentJavaScriptExecutionCommand : ICommand<bool>, IPermissionedCommand
{
    /// <summary>
    /// Per-document gate that authorizes pdfjs to execute embedded JavaScript when the document
    /// is opened in the SPA viewer. Default is OFF: even though the upload preserves JS bytes
    /// byte-for-byte, the viewer reads <c>doc.allowJavaScriptExecution</c> and passes it through
    /// to pdfjs's <c>enableScripting</c> option, so JS stays inert until a privileged operator
    /// explicitly turns it on. Every transition is logged via the <c>[PhiAccess]</c> attribute
    /// on the controller endpoint so a regulator can trace authorization.
    /// </summary>
    public SetDocumentJavaScriptExecutionCommand(Guid DocumentId, bool Allow)
    {
        this.DocumentId = DocumentId;
        this.Allow = Allow;
    }

    // Reuse the retention-administer permission so the same role that controls retention
    // policy controls active-content authorization. Adding a dedicated permission would mean
    // touching the Keycloak realm + every host's RolePermissionMap; the retention role already
    // signals "trusted operator with elevated document responsibilities."
    public string RequiredPermission => HiePermissions.DocumentsRetentionAdminister;
    public Guid DocumentId { get; init; }
    public bool Allow { get; init; }
    public void Deconstruct(out Guid DocumentId, out bool Allow)
    {
        DocumentId = this.DocumentId;
        Allow = this.Allow;
    }
}

public sealed class SetDocumentJavaScriptExecutionCommandHandler : ICommandHandler<SetDocumentJavaScriptExecutionCommand, bool>
{
    private readonly IDocumentReferenceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    public SetDocumentJavaScriptExecutionCommandHandler(IDocumentReferenceRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
    public async Task<bool> HandleAsync(SetDocumentJavaScriptExecutionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var document = await _repository.FindAsync(request.DocumentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Document {request.DocumentId} not found.");
        document.SetJavaScriptExecution(request.Allow);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return document.AllowJavaScriptExecution;
    }
}
