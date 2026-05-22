using Dialysis.HIE.Inbound.Domain;

namespace Dialysis.HIE.Inbound.Ports;

public interface IReceivedResourceStore
{
    Task UpsertAsync(ReceivedResource resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Operator dashboard read — most-recent inbound resources, optionally filtered to one
    /// partner. The view sorts by <c>ReceivedAtUtc</c> descending so the latest delivery is
    /// first.
    /// </summary>
    Task<IReadOnlyList<ReceivedResource>> ListRecentAsync(string? partnerId, int take, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
