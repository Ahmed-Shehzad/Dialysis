using System.Security.Cryptography;
using System.Text;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.TreatmentSessions.Adapters;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Dialysis.SmartConnect.Contracts.Integration;
using Microsoft.Extensions.Logging;

namespace Dialysis.PDMS.TreatmentSessions.Features.IngestMachineTelemetry;

/// <summary>
/// SmartConnect → PDMS boundary consumer. The translator
/// (<see cref="SmartConnectAlarmTranslator"/>, the Anticorruption Layer at Evans pp. 258–260)
/// upcasts the upstream wire event into a PDMS-local <see cref="IncomingAlarm"/>; this
/// consumer then resolves-or-raises the matching <see cref="TreatmentAlarm"/> aggregate and
/// applies the state transition.
/// </summary>
/// <remarks>
/// The aggregate identifies a machine by <see cref="Guid"/>, but the wire event carries a
/// serial string. Until a real Machine catalog aggregate exists we synthesise a deterministic
/// id from the serial via MD5 — this is not a security hash, just a stable serial → Guid map.
/// Same serial always resolves to the same machine id across processes and restarts, so
/// subsequent alarms for the same machine collapse onto the existing aggregate.
/// </remarks>
public sealed class TreatmentAlarmConsumer(
    ITreatmentAlarmRepository alarms,
    IUnitOfWork unitOfWork,
    ILogger<TreatmentAlarmConsumer> logger)
    : IConsumer<DialysisMachineAlarmIntegrationEvent>
{
    public async Task HandleAsync(ConsumeContext<DialysisMachineAlarmIntegrationEvent> context)
    {
        var translated = SmartConnectAlarmTranslator.Translate(context.Message);
        var machineId = SynthesiseMachineId(translated.MachineSerial);

        var existing = await alarms
            .FindLiveAsync(machineId, translated.AlarmCode, context.CancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            // No live aggregate. Only open a new one on an activation — anything else is a
            // late state-change for an aggregate we never saw, so drop it with a log line.
            if (translated.State != TreatmentAlarmState.Present)
            {
                logger.LogInformation(
                    "[ACL] Dropping {State} for unknown alarm: machine {Serial} code {AlarmCode}.",
                    translated.State, translated.MachineSerial, translated.AlarmCode);
                return;
            }

            var raised = TreatmentAlarm.Raise(
                id: Guid.CreateVersion7(),
                sessionId: null,
                machineId: machineId,
                alarmCode: translated.AlarmCode,
                alarmSource: translated.AlarmSource,
                alarmPhase: translated.AlarmPhase,
                observedAtUtc: translated.ObservedAtUtc);
            alarms.Add(raised);
        }
        else
        {
            switch (translated.State)
            {
                case TreatmentAlarmState.Present:
                    existing.Refresh(translated.ObservedAtUtc);
                    break;
                case TreatmentAlarmState.Inactivating:
                    existing.MarkInactivating(translated.ObservedAtUtc);
                    break;
                case TreatmentAlarmState.Resolved:
                    existing.MarkResolved(translated.ObservedAtUtc);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown incoming state {translated.State}.");
            }
        }

        await unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deterministic Guid from the machine serial. Same serial always produces the same id
    /// so independent alarms from the same machine converge on a single aggregate. MD5 here
    /// is purely a fixed-length hash function; no security claim is made.
    /// </summary>
    private static Guid SynthesiseMachineId(string serial)
    {
        var bytes = Encoding.UTF8.GetBytes($"dialysis-machine:{serial}");
        Span<byte> hash = stackalloc byte[16];
        MD5.HashData(bytes, hash);
        return new Guid(hash);
    }
}
