using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.EHR.PatientPortal.Ports;

namespace Dialysis.EHR.PatientPortal.Features.RequestAppointment;

public sealed class RequestAppointmentCommandHandler : ICommandHandler<RequestAppointmentCommand, Guid>
{
    private readonly IPortalAppointmentRequestRepository _requests;
    private readonly IUnitOfWork _unitOfWork;
    public RequestAppointmentCommandHandler(IPortalAppointmentRequestRepository requests,
        IUnitOfWork unitOfWork)
    {
        _requests = requests;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(RequestAppointmentCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var portalRequest = PortalAppointmentRequest.Submit(
            id,
            request.PatientId,
            request.ReasonText,
            request.EarliestPreferredUtc,
            request.LatestPreferredUtc);
        _requests.Add(portalRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
