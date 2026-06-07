using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientPortal.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientPortal.Features.ResolveAppointmentRequest;

/// <summary>
/// Staff approve a patient's appointment request, linking the booked appointment. The appointment
/// itself is created by the controller (which orchestrates the Scheduling slice's BookAppointment);
/// this command only transitions the request and raises the patient-facing resolved event.
/// </summary>
public sealed record ApproveAppointmentRequestCommand(
    Guid RequestId, Guid CreatedAppointmentId, string? StaffNote)
    : ICommand<Unit>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.PortalAppointmentManage;
}

public sealed class ApproveAppointmentRequestCommandHandler
    : ICommandHandler<ApproveAppointmentRequestCommand, Unit>
{
    private readonly IPortalAppointmentRequestRepository _requests;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveAppointmentRequestCommandHandler(
        IPortalAppointmentRequestRepository requests, IUnitOfWork unitOfWork)
    {
        _requests = requests;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> HandleAsync(ApproveAppointmentRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _requests.GetAsync(request.RequestId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException($"Appointment request {request.RequestId} not found.");
        entity.Approve(request.CreatedAppointmentId, request.StaffNote);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
