using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.Operations.Domain.Enumerations;

namespace Dialysis.HIS.Operations.Ports;

public interface IBillingExportJobRepository
{
    void Add(BillingExportJob job);

    Task<BillingExportJob?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BillingExportJob>> ListByStatusAsync(
        BillingExportJobStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Operator-dashboard list — optionally narrowed to one status, bounded by
    /// <paramref name="take"/> (1–500). Sorted newest-first by submission time so the
    /// most recent jobs land at the top of the queue board.
    /// </summary>
    Task<IReadOnlyList<BillingExportJob>> ListAsync(
        BillingExportJobStatus? status,
        int take,
        CancellationToken cancellationToken = default);
}
