using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.Operations.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfStaffRepository(HisDbContext db) : IStaffRepository
{
    public Task<StaffMember?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.StaffMembers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Update(StaffMember member) => db.StaffMembers.Update(member);
}
