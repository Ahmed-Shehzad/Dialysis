using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.LinkImagingStudy;

/// <summary>
/// Records the fulfilled DICOM study back onto an imaging order and completes it — the same step the
/// <c>ImagingStudyLinkedConsumer</c> performs when SmartConnect DICOM STOWs a study, exposed as a
/// direct command so a result can be linked without a full DICOM round-trip (used by the data
/// simulator to close the imaging order → result loop the EHR chart renders).
/// </summary>
public sealed record LinkImagingStudyCommand(
    Guid ImagingOrderId,
    string StudyInstanceUid) : ICommand, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.ImagingOrder;
}

/// <summary>Loads the order, links the study UID, and persists — leaving it <c>Completed</c>.</summary>
public sealed class LinkImagingStudyCommandHandler : ICommandHandler<LinkImagingStudyCommand, Unit>
{
    private readonly IImagingOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Creates the handler.</summary>
    public LinkImagingStudyCommandHandler(IImagingOrderRepository orders, IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Unit> HandleAsync(LinkImagingStudyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var order = await _orders.GetAsync(request.ImagingOrderId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Imaging order not found.");

        // Idempotent: re-linking the same study is a no-op (mirrors the consumer's guard).
        if (string.Equals(order.StudyInstanceUid, request.StudyInstanceUid, StringComparison.Ordinal))
            return Unit.Value;

        order.LinkStudy(request.StudyInstanceUid);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
