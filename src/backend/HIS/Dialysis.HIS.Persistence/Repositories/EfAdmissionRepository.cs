using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfAdmissionRepository(HisDbContext db) : IAdmissionRepository
{
    public void Add(Admission admission) => db.Admissions.Add(admission);

    public Task<Admission?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Admissions.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
}
