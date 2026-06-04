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
public sealed class HaemodialysisSessionOpenEhrConsumer : IConsumer<HaemodialysisSessionProjectedAsOpenEhrIntegrationEvent>
{
    private readonly ICompositionStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<HaemodialysisSessionOpenEhrConsumer> _logger;
    /// <summary>
    /// Lands PDMS-side haemodialysis-session openEHR projections (Started / Completed / Aborted phases)
    /// into the longitudinal composition store. Each phase becomes its own composition version so the
    /// store retains the full session lifecycle, matching openEHR's append-only versioning model.
    /// </summary>
    public HaemodialysisSessionOpenEhrConsumer(ICompositionStore store,
        TimeProvider timeProvider,
        ILogger<HaemodialysisSessionOpenEhrConsumer> logger)
    {
        _store = store;
        _timeProvider = timeProvider;
        _logger = logger;
    }
    private const string ComposerSystem = "pdms.treatment-sessions";

    public async Task HandleAsync(ConsumeContext<HaemodialysisSessionProjectedAsOpenEhrIntegrationEvent> context)
    {
        var msg = context.Message;
        var version = await _store
            .NextVersionAsync(msg.PatientId, msg.ArchetypeId, context.CancellationToken)
            .ConfigureAwait(false);

        var composition = new Composition(
            msg.PatientId,
            msg.ArchetypeId,
            version,
            ComposerSystem,
            _timeProvider.GetUtcNow().UtcDateTime,
            msg.CompositionJson);

        await _store.AddAsync(composition, context.CancellationToken).ConfigureAwait(false);
        _logger.LogDebug(
            "Stored openEHR composition {ArchetypeId} v{Version} ({Phase}) for patient {PatientId} from session {SessionId}",
            msg.ArchetypeId, version, msg.Phase, msg.PatientId, msg.SessionId);
    }
}
