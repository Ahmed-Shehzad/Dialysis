using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.EHR.PatientPortal.Ports;

namespace Dialysis.EHR.PatientPortal.Features.RequestAppointment;

public sealed class RequestAppointmentCommandHandler(
    IPortalAppointmentRequestRepository requests,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RequestAppointmentCommand, Guid>
{
    public async Task<Guid> HandleAsync(RequestAppointmentCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var portalRequest = PortalAppointmentRequest.Submit(
            id,
            request.PatientId,
            request.ReasonText,
            request.EarliestPreferredUtc,
            request.LatestPreferredUtc);
        requests.Add(portalRequest);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
