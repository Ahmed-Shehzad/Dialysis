using Dialysis.BuildingBlocks.Transponder;
using Dialysis.PDMS.TreatmentSessions.Adapters;
using Dialysis.SmartConnect.Contracts.Integration;
using Microsoft.Extensions.Logging;

namespace Dialysis.PDMS.TreatmentSessions.Features.IngestMachineTelemetry;

/// <summary>
/// SmartConnect → PDMS boundary consumer. Translates the upstream
/// <see cref="DialysisMachineAlarmIntegrationEvent"/> via <see cref="SmartConnectAlarmTranslator"/> (the
/// named Anticorruption Layer, Evans pp. 258–260) before any PDMS-side processing. Phase B fills in
/// machine/session resolution + <c>TreatmentAlarm</c> aggregate persistence on top of the translated
/// <see cref="IncomingAlarm"/>; until then the consumer logs the translated intent so the boundary is
/// observable in tests and operations.
/// </summary>
public sealed class TreatmentAlarmConsumer(ILogger<TreatmentAlarmConsumer> logger)
    : IConsumer<DialysisMachineAlarmIntegrationEvent>
{
    public Task HandleAsync(ConsumeContext<DialysisMachineAlarmIntegrationEvent> context)
    {
        var translated = SmartConnectAlarmTranslator.Translate(context.Message);
        logger.LogInformation(
            "[ACL] Incoming alarm: machine {Serial} code {AlarmCode} state {State} observed {ObservedAt} (message-id {MessageControlId}).",
            translated.MachineSerial,
            translated.AlarmCode,
            translated.State,
            translated.ObservedAtUtc,
            translated.MessageControlId);
        return Task.CompletedTask;
    }
}
