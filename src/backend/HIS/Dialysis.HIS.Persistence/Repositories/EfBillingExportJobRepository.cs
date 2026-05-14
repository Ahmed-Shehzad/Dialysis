using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.Operations.Domain.Enumerations;
using Dialysis.HIS.Operations.Domain.Specifications;
using Dialysis.HIS.Operations.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfBillingExportJobRepository(HisDbContext db) : IBillingExportJobRepository
{
    public void Add(BillingExportJob job) => db.BillingExportJobs.Add(job);

    public Task<BillingExportJob?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.BillingExportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BillingExportJob>> ListByStatusAsync(
        BillingExportJobStatus status,
        CancellationToken cancellationToken = default)
    {
        var spec = new BillingExportJobByStatusSpecification(status);
        return await db.BillingExportJobs
            .AsNoTracking()
            .Where(spec.ToExpression())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
