using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.TreatmentSessions.Ports;

namespace Dialysis.PDMS.TreatmentSessions.Features.AcknowledgeAlarm;

public sealed class AcknowledgeAlarmCommandHandler(
    ITreatmentAlarmRepository alarms,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AcknowledgeAlarmCommand, Unit>
{
    public async Task<Unit> HandleAsync(AcknowledgeAlarmCommand request, CancellationToken cancellationToken)
    {
        var alarm = await alarms.GetAsync(request.AlarmId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Alarm '{request.AlarmId}' not found.");

        alarm.Acknowledge(timeProvider.GetUtcNow().UtcDateTime, request.AcknowledgedBy);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
