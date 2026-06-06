using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientPortal.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientPortal.Features.CancelAppointmentRequest;

/// <summary>Patient cancels their own still-pending appointment request.</summary>
public sealed record CancelAppointmentRequestCommand(Guid RequestId)
    : ICommand<Unit>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.PortalAppointmentRequest;
}

public sealed class CancelAppointmentRequestCommandHandler
    : ICommandHandler<CancelAppointmentRequestCommand, Unit>
{
    private readonly IPortalAppointmentRequestRepository _requests;
    private readonly IUnitOfWork _unitOfWork;

    public CancelAppointmentRequestCommandHandler(
        IPortalAppointmentRequestRepository requests, IUnitOfWork unitOfWork)
    {
        _requests = requests;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> HandleAsync(CancelAppointmentRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _requests.GetAsync(request.RequestId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return Unit.Value;
        entity.Cancel();
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
