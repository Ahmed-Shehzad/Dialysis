using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Scheduling.Ports;

namespace Dialysis.EHR.Scheduling.Features.CancelAppointment;

public sealed class CancelAppointmentCommandHandler : ICommandHandler<CancelAppointmentCommand, Unit>
{
    private readonly IAppointmentRepository _appointments;
    private readonly IUnitOfWork _unitOfWork;
    public CancelAppointmentCommandHandler(IAppointmentRepository appointments,
        IUnitOfWork unitOfWork)
    {
        _appointments = appointments;
        _unitOfWork = unitOfWork;
    }
    public async Task<Unit> HandleAsync(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetAsync(request.AppointmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Appointment '{request.AppointmentId}' not found.");
        appointment.Cancel(request.ReasonCode);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
