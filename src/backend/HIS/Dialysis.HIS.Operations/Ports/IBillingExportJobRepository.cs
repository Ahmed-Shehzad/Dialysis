using Dialysis.HIS.Operations.Domain;

namespace Dialysis.HIS.Operations.Ports;

public interface IBillingExportJobRepository
{
    void Add(BillingExportJob job);

    Task<BillingExportJob?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
