using Dialysis.BuildingBlocks.Transponder;
using Dialysis.SmartConnect.Contracts.Integration;
using Microsoft.Extensions.Logging;

namespace Dialysis.PDMS.TreatmentSessions.Features.IngestMachineTelemetry;

/// <summary>
/// Phase A stub. Consumes <see cref="DialysisMachineAlarmIntegrationEvent"/> from SmartConnect and logs the
/// event without driving the alarm state machine. Phase B replaces the body with: resolve current
/// <c>DialysisSession</c>, find-or-raise <c>TreatmentAlarm</c> by (machine, alarm-code, active), refresh /
/// inactivate / resolve per <see cref="DialysisMachineAlarmIntegrationEvent.State"/>, persist.
/// </summary>
public sealed class TreatmentAlarmConsumer(ILogger<TreatmentAlarmConsumer> logger)
    : IConsumer<DialysisMachineAlarmIntegrationEvent>
{
    public Task Handle(ConsumeContext<DialysisMachineAlarmIntegrationEvent> context)
    {
        var alarm = context.Message;
        logger.LogInformation(
            "[Phase A stub] Received alarm {AlarmCode} from machine {Serial} in state {State} at {ObservedAt}, message-id {MessageControlId}.",
            alarm.AlarmCode,
            alarm.MachineSerial,
            alarm.State,
            alarm.ObservedAtUtc,
            alarm.MessageControlId);
        return Task.CompletedTask;
    }
}
