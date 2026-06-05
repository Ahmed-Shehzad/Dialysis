using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Dialysis.BuildingBlocks.DataProtection.Restriction;

namespace Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;

/// <summary>
/// Default <see cref="IDataSubjectRightsService"/> that wires the Art. 15 / 17 / 18 / 20
/// flows to the per-module participation hooks:
/// <list type="bullet">
///   <item>Art. 15 / 20 → walks every registered <see cref="IModuleDataExtractor"/>.</item>
///   <item>Art. 17 — request → file via <see cref="IErasureRequestStore"/>.</item>
///   <item>Art. 17 — approve → run every registered <see cref="IPatientEraser"/> in sequence
///     and persist the composite outcome on the request row.</item>
///   <item>Art. 17 — reject → mark the row rejected with the operator's reason.</item>
///   <item>Art. 18 — restriction → file via <see cref="IRestrictionRequestStore"/>; lifted by an operator.</item>
/// </list>
/// </summary>
public sealed class DefaultDataSubjectRightsService : IDataSubjectRightsService
{
    private readonly IEnumerable<IModuleDataExtractor> _extractors;
    private readonly IEnumerable<IPatientEraser> _erasers;
    private readonly IErasureRequestStore _requestStore;
    private readonly IRestrictionRequestStore _restrictionStore;
    private readonly TimeProvider _clock;

    public DefaultDataSubjectRightsService(
        IEnumerable<IModuleDataExtractor> extractors,
        IEnumerable<IPatientEraser> erasers,
        IErasureRequestStore requestStore,
        IRestrictionRequestStore restrictionStore,
        TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(extractors);
        ArgumentNullException.ThrowIfNull(erasers);
        ArgumentNullException.ThrowIfNull(requestStore);
        ArgumentNullException.ThrowIfNull(restrictionStore);
        ArgumentNullException.ThrowIfNull(clock);
        _extractors = extractors;
        _erasers = erasers;
        _requestStore = requestStore;
        _restrictionStore = restrictionStore;
        _clock = clock;
    }

    public async Task<DataSubjectExport> ExportAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var resources = new List<DataSubjectResource>();
        foreach (var extractor in _extractors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var module = await extractor.ExtractAsync(patientId, cancellationToken).ConfigureAwait(false);
            resources.AddRange(module);
        }
        return new DataSubjectExport(patientId, _clock.GetUtcNow(), resources);
    }

    public async Task<Guid> RequestErasureAsync(
        Guid patientId, string requestedBy, string? reason, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedBy);
        var request = new ErasureRequest(
            Id: Guid.CreateVersion7(),
            PatientId: patientId,
            Status: ErasureRequestStatus.Pending,
            RequestedBy: requestedBy,
            RequestedAtUtc: _clock.GetUtcNow(),
            Reason: reason,
            DecisionBy: null,
            DecisionAtUtc: null,
            DecisionReason: null,
            ExecutionLog: []);
        await _requestStore.SaveAsync(request, cancellationToken).ConfigureAwait(false);
        return request.Id;
    }

    public async Task<ErasureRequest> ApproveErasureRequestAsync(
        Guid requestId, string approvedBy, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedBy);
        var existing = await _requestStore.FindAsync(requestId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Erasure request '{requestId}' not found.");
        if (existing.Status != ErasureRequestStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Erasure request '{requestId}' is already {existing.Status}.");
        }

        var log = new List<ErasureModuleResult>();
        foreach (var eraser in _erasers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await eraser.EraseAsync(existing.PatientId, approvedBy, cancellationToken)
                .ConfigureAwait(false);
            log.Add(new ErasureModuleResult(eraser.ModuleSlug, result.RecordsErased, result.ByCategory));
        }

        var updated = existing with
        {
            Status = ErasureRequestStatus.Executed,
            DecisionBy = approvedBy,
            DecisionAtUtc = _clock.GetUtcNow(),
            ExecutionLog = log,
        };
        await _requestStore.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return updated;
    }

    public async Task<ErasureRequest> RejectErasureRequestAsync(
        Guid requestId, string rejectedBy, string reason, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rejectedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var existing = await _requestStore.FindAsync(requestId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Erasure request '{requestId}' not found.");
        if (existing.Status != ErasureRequestStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Erasure request '{requestId}' is already {existing.Status}.");
        }

        var updated = existing with
        {
            Status = ErasureRequestStatus.Rejected,
            DecisionBy = rejectedBy,
            DecisionAtUtc = _clock.GetUtcNow(),
            DecisionReason = reason,
        };
        await _requestStore.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return updated;
    }

    public Task<IReadOnlyList<ErasureRequest>> ListPendingErasureRequestsAsync(
        int take, CancellationToken cancellationToken) =>
        _requestStore.ListByStatusAsync(ErasureRequestStatus.Pending, take, cancellationToken);

    public async Task<Guid> RequestRestrictionAsync(
        Guid patientId, string requestedBy, string? reason, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedBy);
        var request = new RestrictionRequest(
            Id: Guid.CreateVersion7(),
            PatientId: patientId,
            Status: RestrictionRequestStatus.Active,
            RequestedBy: requestedBy,
            RequestedAtUtc: _clock.GetUtcNow(),
            Reason: reason,
            LiftedBy: null,
            LiftedAtUtc: null,
            LiftReason: null);
        await _restrictionStore.SaveAsync(request, cancellationToken).ConfigureAwait(false);
        return request.Id;
    }

    public Task<IReadOnlyList<RestrictionRequest>> ListActiveRestrictionsAsync(
        int take, CancellationToken cancellationToken) =>
        _restrictionStore.ListByStatusAsync(RestrictionRequestStatus.Active, take, cancellationToken);

    public async Task<RestrictionRequest> LiftRestrictionAsync(
        Guid requestId, string liftedBy, string reason, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(liftedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var existing = await _restrictionStore.FindAsync(requestId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Restriction request '{requestId}' not found.");
        if (existing.Status != RestrictionRequestStatus.Active)
        {
            throw new InvalidOperationException(
                $"Restriction request '{requestId}' is already {existing.Status}.");
        }

        var updated = existing with
        {
            Status = RestrictionRequestStatus.Lifted,
            LiftedBy = liftedBy,
            LiftedAtUtc = _clock.GetUtcNow(),
            LiftReason = reason,
        };
        await _restrictionStore.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return updated;
    }
}
