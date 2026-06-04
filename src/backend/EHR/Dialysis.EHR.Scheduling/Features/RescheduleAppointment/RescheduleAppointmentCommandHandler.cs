using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Scheduling.Ports;

namespace Dialysis.EHR.Scheduling.Features.RescheduleAppointment;

public sealed class RescheduleAppointmentCommandHandler : ICommandHandler<RescheduleAppointmentCommand, Unit>
{
    private readonly IAppointmentRepository _appointments;
    private readonly IUnitOfWork _unitOfWork;
    public RescheduleAppointmentCommandHandler(IAppointmentRepository appointments,
        IUnitOfWork unitOfWork)
    {
        _appointments = appointments;
        _unitOfWork = unitOfWork;
    }
    public async Task<Unit> HandleAsync(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetAsync(request.AppointmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Appointment '{request.AppointmentId}' not found.");
        if (await _appointments.HasOverlapAsync(appointment.ProviderId, request.NewStartUtc, request.NewEndUtc, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Provider has an overlapping appointment in the requested window.");
        appointment.Reschedule(request.NewStartUtc, request.NewEndUtc);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
