using Dialysis.CQRS.Queries;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.ListUserPermissions;

public sealed class ListUserPermissionsQueryHandler(
    IUserAccountRepository users,
    IRoleDefinitionRepository roles,
    IRoleAssignmentRepository assignments)
    : IQueryHandler<ListUserPermissionsQuery, UserPermissionsDto?>
{
    public async Task<UserPermissionsDto?> Handle(ListUserPermissionsQuery request, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return null;

        var userAssignments = await assignments.ListForUserAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var allRoles = await roles.ListAsync(cancellationToken).ConfigureAwait(false);

        var assignedRoles = allRoles.Where(r => userAssignments.Any(a => a.RoleId == r.Id)).ToList();
        var permissions = assignedRoles.SelectMany(r => r.Permissions).Distinct().OrderBy(p => p).ToList();
        var roleCodes = assignedRoles.Select(r => r.Code).ToList();

        return new UserPermissionsDto(user.Id, user.Subject, user.DisplayName, roleCodes, permissions);
    }
}
