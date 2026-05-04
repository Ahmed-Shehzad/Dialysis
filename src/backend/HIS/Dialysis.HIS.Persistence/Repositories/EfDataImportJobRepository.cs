using Dialysis.HIS.DataServices.Domain;
using Dialysis.HIS.DataServices.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfDataImportJobRepository(HisDbContext db) : IDataImportJobRepository
{
    public void Add(DataImportJob job) => db.DataImportJobs.Add(job);

    public Task<DataImportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.DataImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
}
