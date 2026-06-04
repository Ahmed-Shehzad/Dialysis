using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Scheduling.Ports;

namespace Dialysis.EHR.Scheduling.Features.CheckInPatient;

public sealed class CheckInPatientCommandHandler : ICommandHandler<CheckInPatientCommand, Unit>
{
    private readonly IAppointmentRepository _appointments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public CheckInPatientCommandHandler(IAppointmentRepository appointments,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _appointments = appointments;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Unit> HandleAsync(CheckInPatientCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetAsync(request.AppointmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Appointment '{request.AppointmentId}' not found.");
        appointment.CheckIn(_timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
