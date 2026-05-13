using Dialysis.Identity.Provisioning.Domain;

namespace Dialysis.Identity.Provisioning.Ports;

public interface IRoleAssignmentRepository
{
    Task<RoleAssignment?> FindAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleAssignment>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    void Add(RoleAssignment assignment);

    void Remove(RoleAssignment assignment);
}
