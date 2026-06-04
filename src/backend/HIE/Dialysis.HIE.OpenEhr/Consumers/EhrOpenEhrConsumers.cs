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
public sealed class ChartVitalSignOpenEhrConsumer : IConsumer<ChartVitalSignProjectedAsOpenEhrIntegrationEvent>
{
    private readonly ICompositionStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ChartVitalSignOpenEhrConsumer> _logger;
    /// <summary>
    /// Lands EHR-side openEHR projections into the HIE longitudinal composition store.
    /// EHR owns the archetype shape; HIE only assigns the next per-patient version and persists.
    /// </summary>
    public ChartVitalSignOpenEhrConsumer(ICompositionStore store,
        TimeProvider timeProvider,
        ILogger<ChartVitalSignOpenEhrConsumer> logger)
    {
        _store = store;
        _timeProvider = timeProvider;
        _logger = logger;
    }
    private const string ComposerSystem = "ehr.patient-chart";

    public async Task HandleAsync(ConsumeContext<ChartVitalSignProjectedAsOpenEhrIntegrationEvent> context)
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
            "Stored openEHR composition {ArchetypeId} v{Version} for patient {PatientId} from vital reading {ReadingId}",
            msg.ArchetypeId, version, msg.PatientId, msg.VitalSignReadingId);
    }
}

/// <summary>
/// Lands lab-result openEHR projections into the HIE composition store.
/// </summary>
public sealed class LabResultOpenEhrConsumer : IConsumer<LabResultProjectedAsOpenEhrIntegrationEvent>
{
    private readonly ICompositionStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LabResultOpenEhrConsumer> _logger;
    /// <summary>
    /// Lands lab-result openEHR projections into the HIE composition store.
    /// </summary>
    public LabResultOpenEhrConsumer(ICompositionStore store,
        TimeProvider timeProvider,
        ILogger<LabResultOpenEhrConsumer> logger)
    {
        _store = store;
        _timeProvider = timeProvider;
        _logger = logger;
    }
    private const string ComposerSystem = "ehr.integration.lab";

    public async Task HandleAsync(ConsumeContext<LabResultProjectedAsOpenEhrIntegrationEvent> context)
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
            "Stored openEHR composition {ArchetypeId} v{Version} for patient {PatientId} from lab result {ResultId}",
            msg.ArchetypeId, version, msg.PatientId, msg.LabResultId);
    }
}
