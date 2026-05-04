using Dialysis.HIS.Operations.Domain;

namespace Dialysis.HIS.Operations.Ports;

public interface IBillingExportRepository
{
    void Add(BillingExportJob job);

    Task<BillingExportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
