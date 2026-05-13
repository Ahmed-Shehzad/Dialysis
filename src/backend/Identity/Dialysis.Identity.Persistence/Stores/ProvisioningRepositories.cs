using Dialysis.Identity.Provisioning.Domain;
using Dialysis.Identity.Provisioning.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.Identity.Persistence.Stores;

public sealed class UserAccountRepository(IdentityDbContext db) : IUserAccountRepository
{
    public Task<UserAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<UserAccount?> FindBySubjectAsync(string subject, CancellationToken cancellationToken = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Subject == subject, cancellationToken);

    public void Add(UserAccount user) => db.Users.Add(user);

    public void Update(UserAccount user) => db.Users.Update(user);
}

public sealed class RoleDefinitionRepository(IdentityDbContext db) : IRoleDefinitionRepository
{
    public Task<RoleDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<RoleDefinition?> FindByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        db.Roles.FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

    public async Task<IReadOnlyList<RoleDefinition>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Roles.AsNoTracking().OrderBy(r => r.Code).ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(RoleDefinition role) => db.Roles.Add(role);
}

public sealed class RoleAssignmentRepository(IdentityDbContext db) : IRoleAssignmentRepository
{
    public Task<RoleAssignment?> FindAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default) =>
        db.RoleAssignments.FirstOrDefaultAsync(a => a.UserId == userId && a.RoleId == roleId, cancellationToken);

    public async Task<IReadOnlyList<RoleAssignment>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await db.RoleAssignments.AsNoTracking().Where(a => a.UserId == userId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(RoleAssignment assignment) => db.RoleAssignments.Add(assignment);

    public void Remove(RoleAssignment assignment) => db.RoleAssignments.Remove(assignment);
}
