using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfAdmissionRepository : IAdmissionRepository
{
    private readonly HisDbContext _db;
    public EfAdmissionRepository(HisDbContext db) => _db = db;
    public void Add(Admission admission) => _db.Admissions.Add(admission);

    public Task<Admission?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Admissions.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public IAsyncEnumerable<Admission> StreamAllAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        var query = _db.Admissions.AsNoTracking().OrderBy(a => a.AdmittedAtUtc).AsQueryable();
        if (since is { } cutoff)
        {
            var cutoffUtc = cutoff.UtcDateTime;
            query = query.Where(a => (a.DischargedAtUtc ?? a.AdmittedAtUtc) >= cutoffUtc);
        }
        return query.AsAsyncEnumerable();
    }

    public IAsyncEnumerable<Guid> StreamDistinctPatientIdsAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        var query = _db.Admissions.AsNoTracking().AsQueryable();
        if (since is { } cutoff)
        {
            var cutoffUtc = cutoff.UtcDateTime;
            query = query.Where(a => (a.DischargedAtUtc ?? a.AdmittedAtUtc) >= cutoffUtc);
        }
        return query.Select(a => a.PatientId).Distinct().AsAsyncEnumerable();
    }
}
