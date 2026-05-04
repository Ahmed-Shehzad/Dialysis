using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.PatientAccess.Ports;

namespace Dialysis.HIS.PatientAccess.Features.RequestAppointment;

public sealed class RequestPatientAppointmentCommandHandler(
    IPatientAppointmentRequestRepository requests,
    IPatientConsentGate consent,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RequestPatientAppointmentCommand, Guid>
{
    public async Task<Guid> Handle(RequestPatientAppointmentCommand request, CancellationToken cancellationToken)
    {
        await consent.EnsureCanRequestAppointmentAsync(request.PatientId, cancellationToken).ConfigureAwait(false);
        var id = Guid.CreateVersion7();
        requests.Add(new PatientAppointmentRequest
        {
            Id = id,
            PatientId = request.PatientId,
            Notes = request.Notes,
            RequestedAtUtc = DateTime.UtcNow,
        });
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
