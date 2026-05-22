using Dialysis.HIE.Outbound.Domain;

namespace Dialysis.HIE.Outbound.Ports;

public interface IOutboundBundleStore
{
    Task AddAsync(OutboundBundle bundle, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboundBundle>> ClaimPendingAsync(int batchSize, DateTime asOfUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Operator dashboard read — every bundle ordered most-recent first. Optional status
    /// filter narrows the view; <c>null</c> returns rows in every state.
    /// </summary>
    Task<IReadOnlyList<OutboundBundle>> ListAsync(OutboundBundleStatus? statusFilter, int take, CancellationToken cancellationToken = default);

    /// <summary>Loads a single bundle by id for the retry command. Returns <c>null</c> on miss.</summary>
    Task<OutboundBundle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
