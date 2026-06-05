using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.ReviewImagingAiFinding;

/// <summary>
/// The human-in-the-loop sign-off: a clinician accepts or rejects the advisory AI finding attached
/// to an imaging order. <see cref="ReviewedBy"/> is taken from the authenticated user server-side.
/// </summary>
public sealed record ReviewImagingAiFindingCommand(
    Guid ImagingOrderId,
    bool Accepted,
    string ReviewedBy) : ICommand, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.ImagingAiReview;
}

public sealed class ReviewImagingAiFindingCommandHandler : ICommandHandler<ReviewImagingAiFindingCommand, Unit>
{
    private readonly IImagingOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    public ReviewImagingAiFindingCommandHandler(IImagingOrderRepository orders, IUnitOfWork unitOfWork, TimeProvider clock)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Unit> HandleAsync(ReviewImagingAiFindingCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetAsync(request.ImagingOrderId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Imaging order not found.");

        order.ReviewAiFinding(request.Accepted, request.ReviewedBy, _clock.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
