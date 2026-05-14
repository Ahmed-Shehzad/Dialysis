using Dialysis.BuildingBlocks.Transponder;
using Dialysis.HIE.OpenEhr.Domain;
using Dialysis.HIE.OpenEhr.Ports;
using Dialysis.PDMS.Contracts.Integration;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.OpenEhr.Consumers;

/// <summary>
/// Lands PDMS-side haemodialysis-session openEHR projections (Started / Completed / Aborted phases)
/// into the longitudinal composition store. Each phase becomes its own composition version so the
/// store retains the full session lifecycle, matching openEHR's append-only versioning model.
/// </summary>
public sealed class HaemodialysisSessionOpenEhrConsumer(
    ICompositionStore store,
    TimeProvider timeProvider,
    ILogger<HaemodialysisSessionOpenEhrConsumer> logger)
    : IConsumer<HaemodialysisSessionProjectedAsOpenEhrIntegrationEvent>
{
    private const string ComposerSystem = "pdms.treatment-sessions";

    public async Task HandleAsync(ConsumeContext<HaemodialysisSessionProjectedAsOpenEhrIntegrationEvent> context)
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
            "Stored openEHR composition {ArchetypeId} v{Version} ({Phase}) for patient {PatientId} from session {SessionId}",
            msg.ArchetypeId, version, msg.Phase, msg.PatientId, msg.SessionId);
    }
}
