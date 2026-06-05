using Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;

namespace Dialysis.BuildingBlocks.DataProtection.Restriction;

/// <summary>
/// Persistence port for <see cref="RestrictionRequest"/> rows — the Art. 18 sibling of
/// <c>IErasureRequestStore</c>. Each deployment plugs in an EF-backed implementation (HIE) or
/// falls back to the in-memory baseline so the audit trail resolves on every host. The
/// orchestrator in <c>DefaultDataSubjectRightsService</c> mutates the request through the
/// Active → Lifted transition.
/// </summary>
public interface IRestrictionRequestStore
{
    Task SaveAsync(RestrictionRequest request, CancellationToken cancellationToken);

    Task<RestrictionRequest?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<RestrictionRequest>> ListByStatusAsync(
        RestrictionRequestStatus status, int take, CancellationToken cancellationToken);
}
