using System.Security.Cryptography;
using System.Text;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.TreatmentSessions.Adapters;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Microsoft.Extensions.Logging;

namespace Dialysis.PDMS.TreatmentSessions.Features.IngestMachineTelemetry;

/// <summary>
/// Shared resolve-or-raise pipeline for a single <see cref="IncomingAlarm"/>. Both the
/// SmartConnect boundary consumer (<see cref="TreatmentAlarmConsumer"/>) and the direct HTTP
/// ingest command handler funnel through here so a machine alarm follows exactly one code path
/// regardless of whether it arrived over RabbitMQ (from SmartConnect) or over HTTP (operator /
/// device gateway / simulator). The first <c>Present</c> for a (machine, code) opens a new
/// <see cref="TreatmentAlarm"/>; subsequent states refresh / inactivate / resolve the same
/// aggregate, which is what makes the active-alarms board converge instead of duplicating.
/// </summary>
public sealed class TreatmentAlarmIngestionService
{
    private readonly ITreatmentAlarmRepository _alarms;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TreatmentAlarmIngestionService> _logger;

    /// <summary>Creates the ingestion service.</summary>
    public TreatmentAlarmIngestionService(
        ITreatmentAlarmRepository alarms,
        IUnitOfWork unitOfWork,
        ILogger<TreatmentAlarmIngestionService> logger)
    {
        _alarms = alarms;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Applies one incoming alarm state-change to the matching aggregate and persists it.
    /// <paramref name="sessionId"/> tags a newly-raised alarm with the in-progress session it
    /// belongs to (null for purely machine-scoped alarms, as the SmartConnect path supplies).
    /// </summary>
    public async Task ApplyAsync(IncomingAlarm incoming, Guid? sessionId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        var machineId = SynthesiseMachineId(incoming.MachineSerial);

        var existing = await _alarms
            .FindLiveAsync(machineId, incoming.AlarmCode, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            // No live aggregate. Only open a new one on an activation — anything else is a
            // late state-change for an aggregate we never saw, so drop it with a log line.
            if (incoming.State != TreatmentAlarmState.Present)
            {
                _logger.LogInformation(
                    "[ACL] Dropping {State} for unknown alarm: machine {Serial} code {AlarmCode}.",
                    incoming.State, incoming.MachineSerial, incoming.AlarmCode);
                return;
            }

            var raised = TreatmentAlarm.Raise(
                id: Guid.CreateVersion7(),
                sessionId: sessionId,
                machineId: machineId,
                alarmCode: incoming.AlarmCode,
                alarmSource: incoming.AlarmSource,
                alarmPhase: incoming.AlarmPhase,
                observedAtUtc: incoming.ObservedAtUtc);
            _alarms.Add(raised);
        }
        else
        {
            switch (incoming.State)
            {
                case TreatmentAlarmState.Present:
                    existing.Refresh(incoming.ObservedAtUtc);
                    break;
                case TreatmentAlarmState.Inactivating:
                    existing.MarkInactivating(incoming.ObservedAtUtc);
                    break;
                case TreatmentAlarmState.Resolved:
                    existing.MarkResolved(incoming.ObservedAtUtc);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown incoming state {incoming.State}.");
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deterministic Guid from the machine serial. Same serial always produces the same id
    /// so independent alarms from the same machine converge on a single aggregate. MD5 here
    /// is purely a fixed-length hash function; no security claim is made.
    /// </summary>
    public static Guid SynthesiseMachineId(string serial)
    {
        var bytes = Encoding.UTF8.GetBytes($"dialysis-machine:{serial}");
        Span<byte> hash = stackalloc byte[16];
        MD5.HashData(bytes, hash);
        return new Guid(hash);
    }
}
