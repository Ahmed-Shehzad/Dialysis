using Dialysis.BuildingBlocks.Transponder;
using Dialysis.SmartConnect.Contracts.Integration;
using Microsoft.Extensions.Logging;

namespace Dialysis.PDMS.TreatmentSessions.Features.IngestMachineTelemetry;

/// <summary>
/// Phase A stub. Consumes <see cref="DialysisMachineTreatmentSnapshotIntegrationEvent"/> from SmartConnect
/// and logs the event without persisting. Phase B replaces the body with: upsert <c>DialysisMachine</c>,
/// resolve current <c>DialysisSession</c>, append <c>TreatmentObservation</c> rows, persist a
/// <c>RawHl7Message</c> audit row.
/// </summary>
public sealed class TreatmentSnapshotConsumer(ILogger<TreatmentSnapshotConsumer> logger)
    : IConsumer<DialysisMachineTreatmentSnapshotIntegrationEvent>
{
    public Task Handle(ConsumeContext<DialysisMachineTreatmentSnapshotIntegrationEvent> context)
    {
        var snapshot = context.Message;
        logger.LogInformation(
            "[Phase A stub] Received treatment snapshot from machine {Serial}: {ObservationCount} observations at {ObservedAt}, message-id {MessageControlId}.",
            snapshot.MachineSerial,
            snapshot.Observations.Count,
            snapshot.ObservedAtUtc,
            snapshot.MessageControlId);
        return Task.CompletedTask;
    }
}
