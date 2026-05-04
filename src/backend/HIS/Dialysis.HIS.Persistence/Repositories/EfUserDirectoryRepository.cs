using Dialysis.HIS.Security.Domain;
using Dialysis.HIS.Security.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfUserDirectoryRepository(HisDbContext db) : IUserDirectoryRepository
{
    public void AddUser(HisUserAccount user) => db.UserAccounts.Add(user);

    public void AttachRole(HisUserAccount user, HisRole role)
    {
        if (user.UserRoles.Any(ur => ur.RoleId == role.Id))
            return;

        user.UserRoles.Add(new HisUserRole { UserId = user.Id, RoleId = role.Id, User = user, Role = role });
    }

    public Task<HisRole?> FindRoleByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

    public Task<HisUserAccount?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
        db.UserAccounts.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
}
