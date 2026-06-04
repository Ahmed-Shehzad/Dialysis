using Dialysis.HIS.Security.Domain;
using Dialysis.HIS.Security.Domain.ValueObjects;
using Dialysis.HIS.Security.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIS.Persistence.Repositories;

public sealed class EfLocalUserRepository : ILocalUserRepository
{
    private readonly HisDbContext _db;
    public EfLocalUserRepository(HisDbContext db) => _db = db;
    public void Add(LocalUser user) => _db.LocalUsers.Add(user);

    public Task<LocalUser?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.LocalUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<bool> LoginNameExistsAsync(string loginName, CancellationToken cancellationToken = default)
    {
        var ln = new LoginName(loginName);
        return _db.LocalUsers.AsNoTracking()
                  .AnyAsync(u => u.LoginName == ln, cancellationToken);
    }
}
