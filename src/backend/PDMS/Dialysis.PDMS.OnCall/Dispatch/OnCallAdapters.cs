using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.OnCall.Domain;

namespace Dialysis.PDMS.OnCall.Dispatch;

/// <summary>
/// Generic <see cref="IPdmsRepository{TAggregate, TId}"/>-backed lookup for the active
/// rotation. We pull every rotation row for the chair, filter to the one that covers
/// <paramref name="atUtc"/>, and return it. Production deployments should index the
/// rotation table on <c>(ChairId, EffectiveFromUtc, EffectiveUntilUtc)</c> so this stays
/// cheap; the EF configuration in PR 6 declares that index.
/// </summary>
public sealed class PdmsOnCallRotationLookup : IOnCallRotationLookup
{
    private readonly IPdmsRepository<OnCallRotation, Guid> _repository;
    /// <summary>
    /// Generic <see cref="IPdmsRepository{TAggregate, TId}"/>-backed lookup for the active
    /// rotation. We pull every rotation row for the chair, filter to the one that covers
    /// <paramref name="atUtc"/>, and return it. Production deployments should index the
    /// rotation table on <c>(ChairId, EffectiveFromUtc, EffectiveUntilUtc)</c> so this stays
    /// cheap; the EF configuration in PR 6 declares that index.
    /// </summary>
    public PdmsOnCallRotationLookup(IPdmsRepository<OnCallRotation, Guid> repository) => _repository = repository;
    public async Task<OnCallRotation?> FindActiveAsync(Guid chairId, DateTime atUtc, CancellationToken cancellationToken)
    {
        var all = await _repository.ListAsync(null, cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault(r => r.ChairId == chairId && r.CoversInstant(atUtc));
    }
}

/// <summary>
/// Active-escalation-policy lookup. Returns the first row; the platform's contract is
/// "one policy per facility" so any policy stored is the active one until an admin
/// uploads a replacement.
/// </summary>
public sealed class PdmsEscalationPolicyLookup : IEscalationPolicyLookup
{
    private readonly IPdmsRepository<EscalationPolicy, Guid> _repository;
    /// <summary>
    /// Active-escalation-policy lookup. Returns the first row; the platform's contract is
    /// "one policy per facility" so any policy stored is the active one until an admin
    /// uploads a replacement.
    /// </summary>
    public PdmsEscalationPolicyLookup(IPdmsRepository<EscalationPolicy, Guid> repository) => _repository = repository;
    public async Task<EscalationPolicy?> FindActiveAsync(CancellationToken cancellationToken)
    {
        var all = await _repository.ListAsync(null, cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault();
    }
}

/// <summary>
/// Bridges the consumer's <see cref="IAlarmDispatchRepository"/> port onto the generic
/// PDMS repository. Persisting the dispatch + its attempt history is what
/// <c>/admin/oncall/audit</c> reads back through.
/// </summary>
public sealed class PdmsAlarmDispatchRepository : IAlarmDispatchRepository
{
    private readonly IPdmsRepository<AlarmDispatch, Guid> _repository;
    /// <summary>
    /// Bridges the consumer's <see cref="IAlarmDispatchRepository"/> port onto the generic
    /// PDMS repository. Persisting the dispatch + its attempt history is what
    /// <c>/admin/oncall/audit</c> reads back through.
    /// </summary>
    public PdmsAlarmDispatchRepository(IPdmsRepository<AlarmDispatch, Guid> repository) => _repository = repository;
    public Task AddAsync(AlarmDispatch dispatch, CancellationToken cancellationToken)
        => _repository.AddAsync(dispatch, cancellationToken);

    public Task<AlarmDispatch?> FindAsync(Guid dispatchId, CancellationToken cancellationToken)
        => _repository.GetByIdAsync(dispatchId, cancellationToken);
}
