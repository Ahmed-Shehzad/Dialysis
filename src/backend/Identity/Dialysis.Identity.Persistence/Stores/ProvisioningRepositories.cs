using Dialysis.Identity.Provisioning.Domain;
using Dialysis.Identity.Provisioning.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.Identity.Persistence.Stores;

public sealed class UserAccountRepository : IUserAccountRepository
{
    private readonly IdentityDbContext _db;
    public UserAccountRepository(IdentityDbContext db) => _db = db;
    public Task<UserAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<UserAccount?> FindBySubjectAsync(string subject, CancellationToken cancellationToken = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Subject == subject, cancellationToken);

    public void Add(UserAccount user) => _db.Users.Add(user);

    public void Update(UserAccount user) => _db.Users.Update(user);
}

public sealed class RoleDefinitionRepository : IRoleDefinitionRepository
{
    private readonly IdentityDbContext _db;
    public RoleDefinitionRepository(IdentityDbContext db) => _db = db;
    public Task<RoleDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<RoleDefinition?> FindByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

    public async Task<IReadOnlyList<RoleDefinition>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.Roles.AsNoTracking().OrderBy(r => r.Code).ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(RoleDefinition role) => _db.Roles.Add(role);
}

public sealed class RoleAssignmentRepository : IRoleAssignmentRepository
{
    private readonly IdentityDbContext _db;
    public RoleAssignmentRepository(IdentityDbContext db) => _db = db;
    public Task<RoleAssignment?> FindAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default) =>
        _db.RoleAssignments.FirstOrDefaultAsync(a => a.UserId == userId && a.RoleId == roleId, cancellationToken);

    public async Task<IReadOnlyList<RoleAssignment>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _db.RoleAssignments.AsNoTracking().Where(a => a.UserId == userId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(RoleAssignment assignment) => _db.RoleAssignments.Add(assignment);

    public void Remove(RoleAssignment assignment) => _db.RoleAssignments.Remove(assignment);
}
