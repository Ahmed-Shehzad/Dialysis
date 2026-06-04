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
public sealed class TreatmentSnapshotConsumer : IConsumer<DialysisMachineTreatmentSnapshotIntegrationEvent>
{
    private readonly ILogger<TreatmentSnapshotConsumer> _logger;
    /// <summary>
    /// SmartConnect → PDMS boundary consumer for treatment-status snapshots. Translates the upstream
    /// <see cref="DialysisMachineTreatmentSnapshotIntegrationEvent"/> via
    /// <see cref="SmartConnectSnapshotTranslator"/> (the named Anticorruption Layer, Evans pp. 258–260).
    /// Phase B persists each translated <see cref="IncomingObservation"/> as a <c>TreatmentObservation</c>
    /// child of the bound session; until then the consumer logs the translated intent.
    /// </summary>
    public TreatmentSnapshotConsumer(ILogger<TreatmentSnapshotConsumer> logger) => _logger = logger;
    public Task HandleAsync(ConsumeContext<DialysisMachineTreatmentSnapshotIntegrationEvent> context)
    {
        var translated = SmartConnectSnapshotTranslator.Translate(context.Message);
        _logger.LogInformation(
            "[ACL] Incoming treatment snapshot: machine {Serial} mrn {Mrn} observations {Count} observed {ObservedAt} (message-id {MessageControlId}).",
            translated.MachineSerial,
            translated.PatientMrn,
            translated.Observations.Count,
            translated.ObservedAtUtc,
            translated.MessageControlId);
        return Task.CompletedTask;
    }
}
