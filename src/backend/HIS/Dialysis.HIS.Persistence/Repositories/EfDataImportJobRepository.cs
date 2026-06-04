using Dialysis.HIS.DataServices.Domain;
using Dialysis.HIS.DataServices.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfDataImportJobRepository : IDataImportJobRepository
{
    private readonly HisDbContext _db;
    public EfDataImportJobRepository(HisDbContext db) => _db = db;
    public void Add(DataImportJob job) => _db.DataImportJobs.Add(job);

    public Task<DataImportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.DataImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
}
