using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Scheduling.Ports;

namespace Dialysis.EHR.Scheduling.Features.RescheduleAppointment;

public sealed class RescheduleAppointmentCommandHandler(
    IAppointmentRepository appointments,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RescheduleAppointmentCommand, Unit>
{
    public async Task<Unit> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await appointments.GetAsync(request.AppointmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Appointment '{request.AppointmentId}' not found.");
        if (await appointments.HasOverlapAsync(appointment.ProviderId, request.NewStartUtc, request.NewEndUtc, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Provider has an overlapping appointment in the requested window.");
        appointment.Reschedule(request.NewStartUtc, request.NewEndUtc);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
