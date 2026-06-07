using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientPortal.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientPortal.Features.ResolveAppointmentRequest;

/// <summary>Staff decline a patient's appointment request with a note.</summary>
public sealed record DeclineAppointmentRequestCommand(Guid RequestId, string StaffNote)
    : ICommand<Unit>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.PortalAppointmentManage;
}

public sealed class DeclineAppointmentRequestCommandHandler
    : ICommandHandler<DeclineAppointmentRequestCommand, Unit>
{
    private readonly IPortalAppointmentRequestRepository _requests;
    private readonly IUnitOfWork _unitOfWork;

    public DeclineAppointmentRequestCommandHandler(
        IPortalAppointmentRequestRepository requests, IUnitOfWork unitOfWork)
    {
        _requests = requests;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> HandleAsync(DeclineAppointmentRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _requests.GetAsync(request.RequestId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException($"Appointment request {request.RequestId} not found.");
        entity.Decline(request.StaffNote);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
