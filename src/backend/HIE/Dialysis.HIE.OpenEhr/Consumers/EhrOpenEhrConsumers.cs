using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.OpenEhr.Domain;
using Dialysis.HIE.OpenEhr.Ports;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.OpenEhr.Consumers;

/// <summary>
/// Lands EHR-side openEHR projections into the HIE longitudinal composition store.
/// EHR owns the archetype shape; HIE only assigns the next per-patient version and persists.
/// </summary>
public sealed class ChartVitalSignOpenEhrConsumer(
    ICompositionStore store,
    TimeProvider timeProvider,
    ILogger<ChartVitalSignOpenEhrConsumer> logger)
    : IConsumer<ChartVitalSignProjectedAsOpenEhrIntegrationEvent>
{
    private const string ComposerSystem = "ehr.patient-chart";

    public async Task HandleAsync(ConsumeContext<ChartVitalSignProjectedAsOpenEhrIntegrationEvent> context)
    {
        var msg = context.Message;
        var version = await store
            .NextVersionAsync(msg.PatientId, msg.ArchetypeId, context.CancellationToken)
            .ConfigureAwait(false);

        var composition = new Composition(
            msg.PatientId,
            msg.ArchetypeId,
            version,
            ComposerSystem,
            timeProvider.GetUtcNow().UtcDateTime,
            msg.CompositionJson);

        await store.AddAsync(composition, context.CancellationToken).ConfigureAwait(false);
        logger.LogDebug(
            "Stored openEHR composition {ArchetypeId} v{Version} for patient {PatientId} from vital reading {ReadingId}",
            msg.ArchetypeId, version, msg.PatientId, msg.VitalSignReadingId);
    }
}

/// <summary>
/// Lands lab-result openEHR projections into the HIE composition store.
/// </summary>
public sealed class LabResultOpenEhrConsumer(
    ICompositionStore store,
    TimeProvider timeProvider,
    ILogger<LabResultOpenEhrConsumer> logger)
    : IConsumer<LabResultProjectedAsOpenEhrIntegrationEvent>
{
    private const string ComposerSystem = "ehr.integration.lab";

    public async Task HandleAsync(ConsumeContext<LabResultProjectedAsOpenEhrIntegrationEvent> context)
    {
        var msg = context.Message;
        var version = await store
            .NextVersionAsync(msg.PatientId, msg.ArchetypeId, context.CancellationToken)
            .ConfigureAwait(false);

        var composition = new Composition(
            msg.PatientId,
            msg.ArchetypeId,
            version,
            ComposerSystem,
            timeProvider.GetUtcNow().UtcDateTime,
            msg.CompositionJson);

        await store.AddAsync(composition, context.CancellationToken).ConfigureAwait(false);
        logger.LogDebug(
            "Stored openEHR composition {ArchetypeId} v{Version} for patient {PatientId} from lab result {ResultId}",
            msg.ArchetypeId, version, msg.PatientId, msg.LabResultId);
    }
}
