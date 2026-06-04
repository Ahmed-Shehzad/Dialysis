using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.TreatmentSessions.Ports;

namespace Dialysis.PDMS.TreatmentSessions.Features.AcknowledgeAlarm;

public sealed class AcknowledgeAlarmCommandHandler : ICommandHandler<AcknowledgeAlarmCommand, Unit>
{
    private readonly ITreatmentAlarmRepository _alarms;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public AcknowledgeAlarmCommandHandler(ITreatmentAlarmRepository alarms,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _alarms = alarms;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Unit> HandleAsync(AcknowledgeAlarmCommand request, CancellationToken cancellationToken)
    {
        var alarm = await _alarms.GetAsync(request.AlarmId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Alarm '{request.AlarmId}' not found.");

        alarm.Acknowledge(_timeProvider.GetUtcNow().UtcDateTime, request.AcknowledgedBy);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
