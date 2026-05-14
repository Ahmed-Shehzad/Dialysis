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
}
