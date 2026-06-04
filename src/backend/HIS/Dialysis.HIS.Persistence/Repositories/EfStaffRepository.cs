using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.Operations.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfStaffRepository : IStaffRepository
{
    private readonly HisDbContext _db;
    public EfStaffRepository(HisDbContext db) => _db = db;
    public Task<StaffMember?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.StaffMembers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Update(StaffMember member) => _db.StaffMembers.Update(member);
}
