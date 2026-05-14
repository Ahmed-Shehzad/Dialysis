using Dialysis.BuildingBlocks.Transponder;
using Dialysis.PDMS.TreatmentSessions.Adapters;
using Dialysis.SmartConnect.Contracts.Integration;
using Microsoft.Extensions.Logging;

namespace Dialysis.PDMS.TreatmentSessions.Features.IngestMachineTelemetry;

/// <summary>
/// SmartConnect → PDMS boundary consumer for treatment-status snapshots. Translates the upstream
/// <see cref="DialysisMachineTreatmentSnapshotIntegrationEvent"/> via
/// <see cref="SmartConnectSnapshotTranslator"/> (the named Anticorruption Layer, Evans pp. 258–260).
/// Phase B persists each translated <see cref="IncomingObservation"/> as a <c>TreatmentObservation</c>
/// child of the bound session; until then the consumer logs the translated intent.
/// </summary>
public sealed class TreatmentSnapshotConsumer(ILogger<TreatmentSnapshotConsumer> logger)
    : IConsumer<DialysisMachineTreatmentSnapshotIntegrationEvent>
{
    public Task Handle(ConsumeContext<DialysisMachineTreatmentSnapshotIntegrationEvent> context)
    {
        var translated = SmartConnectSnapshotTranslator.Translate(context.Message);
        logger.LogInformation(
            "[ACL] Incoming treatment snapshot: machine {Serial} mrn {Mrn} observations {Count} observed {ObservedAt} (message-id {MessageControlId}).",
            translated.MachineSerial,
            translated.PatientMrn,
            translated.Observations.Count,
            translated.ObservedAtUtc,
            translated.MessageControlId);
        return Task.CompletedTask;
    }
}
