using Dialysis.HIS.Security.Domain;
using Dialysis.HIS.Security.Domain.ValueObjects;
using Dialysis.HIS.Security.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfLocalUserRepository(HisDbContext db) : ILocalUserRepository
{
    public void Add(LocalUser user) => db.LocalUsers.Add(user);

    public Task<LocalUser?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => db.LocalUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<bool> LoginNameExistsAsync(string loginName, CancellationToken cancellationToken = default)
    {
        var ln = new LoginName(loginName);
        return db.LocalUsers.AsNoTracking()
                  .AnyAsync(u => u.LoginName == ln, cancellationToken);
    }
}
