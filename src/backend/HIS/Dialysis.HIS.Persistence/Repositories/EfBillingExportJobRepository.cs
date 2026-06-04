using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.Operations.Domain.Enumerations;
using Dialysis.HIS.Operations.Domain.Specifications;
using Dialysis.HIS.Operations.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfBillingExportJobRepository : IBillingExportJobRepository
{
    private readonly HisDbContext _db;
    public EfBillingExportJobRepository(HisDbContext db) => _db = db;
    public void Add(BillingExportJob job) => _db.BillingExportJobs.Add(job);

    public Task<BillingExportJob?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.BillingExportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BillingExportJob>> ListByStatusAsync(
        BillingExportJobStatus status,
        CancellationToken cancellationToken = default)
    {
        var spec = new BillingExportJobByStatusSpecification(status);
        return await _db.BillingExportJobs
            .AsNoTracking()
            .Where(spec.ToExpression())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BillingExportJob>> ListAsync(
        BillingExportJobStatus? status,
        int take,
        CancellationToken cancellationToken = default)
    {
        var bounded = Math.Clamp(take, 1, 500);
        var query = _db.BillingExportJobs.AsNoTracking();
        if (status is not null)
        {
            var spec = new BillingExportJobByStatusSpecification(status);
            query = query.Where(spec.ToExpression());
        }
        return await query
            .OrderByDescending(j => j.SubmittedAtUtc)
            .Take(bounded)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
