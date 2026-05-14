using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Scheduling.Ports;

namespace Dialysis.EHR.Scheduling.Features.CheckInPatient;

public sealed class CheckInPatientCommandHandler(
    IAppointmentRepository appointments,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<CheckInPatientCommand, Unit>
{
    public async Task<Unit> HandleAsync(CheckInPatientCommand request, CancellationToken cancellationToken)
    {
        var appointment = await appointments.GetAsync(request.AppointmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Appointment '{request.AppointmentId}' not found.");
        appointment.CheckIn(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
