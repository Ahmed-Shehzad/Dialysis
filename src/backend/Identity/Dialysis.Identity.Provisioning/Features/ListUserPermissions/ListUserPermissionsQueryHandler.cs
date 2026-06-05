using Dialysis.CQRS.Queries;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.ListUserPermissions;

public sealed class ListUserPermissionsQueryHandler : IQueryHandler<ListUserPermissionsQuery, UserPermissionsDto?>
{
    private readonly IUserAccountRepository _users;
    private readonly IRoleDefinitionRepository _roles;
    private readonly IRoleAssignmentRepository _assignments;
    public ListUserPermissionsQueryHandler(IUserAccountRepository users,
        IRoleDefinitionRepository roles,
        IRoleAssignmentRepository assignments)
    {
        _users = users;
        _roles = roles;
        _assignments = assignments;
    }
    public async Task<UserPermissionsDto?> HandleAsync(ListUserPermissionsQuery request, CancellationToken cancellationToken)
    {
        var user = await _users.GetAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return null;

        var userAssignments = await _assignments.ListForUserAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var roleIds = userAssignments.Select(a => a.RoleId).Distinct().ToArray();
        var assignedRoles = await _roles.ListByIdsAsync(roleIds, cancellationToken).ConfigureAwait(false);
        var permissions = assignedRoles.SelectMany(r => r.Permissions).Distinct().OrderBy(p => p).ToList();
        var roleCodes = assignedRoles.Select(r => r.Code).ToList();

        return new UserPermissionsDto(user.Id, user.Subject, user.DisplayName, roleCodes, permissions);
    }
}
