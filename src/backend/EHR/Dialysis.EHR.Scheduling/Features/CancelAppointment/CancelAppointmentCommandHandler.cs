using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Scheduling.Ports;

namespace Dialysis.EHR.Scheduling.Features.CancelAppointment;

public sealed class CancelAppointmentCommandHandler(
    IAppointmentRepository appointments,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelAppointmentCommand, Unit>
{
    public async Task<Unit> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await appointments.GetAsync(request.AppointmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Appointment '{request.AppointmentId}' not found.");
        appointment.Cancel(request.ReasonCode);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
