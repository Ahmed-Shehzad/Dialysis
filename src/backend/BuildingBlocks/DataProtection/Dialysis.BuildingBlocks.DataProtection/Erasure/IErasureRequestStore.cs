using Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;

namespace Dialysis.BuildingBlocks.DataProtection.Erasure;

/// <summary>
/// Persistence port for <see cref="ErasureRequest"/> rows. Each deployment plugs in an
/// EF-backed implementation (or any other store) so the audit trail survives host restarts.
/// The store is intentionally minimal — the orchestrator in
/// <c>DefaultDataSubjectRightsService</c> mutates the request through the well-known
/// status transitions.
/// </summary>
public interface IErasureRequestStore
{
    Task SaveAsync(ErasureRequest request, CancellationToken cancellationToken);

    Task<ErasureRequest?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ErasureRequest>> ListByStatusAsync(
        ErasureRequestStatus status, int take, CancellationToken cancellationToken);
}
