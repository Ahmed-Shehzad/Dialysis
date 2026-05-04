using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.Operations.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfBillingExportRepository(HisDbContext db) : IBillingExportRepository
{
    public void Add(BillingExportJob job) => db.BillingExportJobs.Add(job);

    public Task<BillingExportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.BillingExportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
}
