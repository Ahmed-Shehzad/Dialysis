using Dialysis.BuildingBlocks.Transponder;
using Dialysis.PDMS.TreatmentSessions.Adapters;
using Dialysis.SmartConnect.Contracts.Integration;

namespace Dialysis.PDMS.TreatmentSessions.Features.IngestMachineTelemetry;

/// <summary>
/// SmartConnect → PDMS boundary consumer. The translator
/// (<see cref="SmartConnectAlarmTranslator"/>, the Anticorruption Layer at Evans pp. 258–260)
/// upcasts the upstream wire event into a PDMS-local <see cref="IncomingAlarm"/>; the shared
/// <see cref="TreatmentAlarmIngestionService"/> then resolves-or-raises the matching
/// <see cref="Domain.TreatmentAlarm"/> aggregate and applies the state transition. The same
/// service backs the direct HTTP ingest path so both arrival channels behave identically.
/// </summary>
public sealed class TreatmentAlarmConsumer : IConsumer<DialysisMachineAlarmIntegrationEvent>
{
    private readonly TreatmentAlarmIngestionService _ingestion;

    /// <summary>Creates the consumer.</summary>
    public TreatmentAlarmConsumer(TreatmentAlarmIngestionService ingestion) => _ingestion = ingestion;

    /// <inheritdoc />
    public async Task HandleAsync(ConsumeContext<DialysisMachineAlarmIntegrationEvent> context)
    {
        var translated = SmartConnectAlarmTranslator.Translate(context.Message);
        // Machine alarms from the wire are machine-scoped, not session-scoped (no session id on the event).
        await _ingestion.ApplyAsync(translated, sessionId: null, context.CancellationToken).ConfigureAwait(false);
    }
}
