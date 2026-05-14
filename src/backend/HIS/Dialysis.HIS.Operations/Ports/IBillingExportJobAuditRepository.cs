using Dialysis.HIS.Operations.Domain;

namespace Dialysis.HIS.Operations.Ports;

public interface IBillingExportJobAuditRepository
{
    Task RecordAsync(BillingExportJobAudit audit, CancellationToken cancellationToken = default);
}
