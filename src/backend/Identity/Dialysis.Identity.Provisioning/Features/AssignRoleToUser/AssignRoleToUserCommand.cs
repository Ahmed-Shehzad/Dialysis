using Dialysis.CQRS.Commands;
using Dialysis.Identity.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Identity.Provisioning.Features.AssignRoleToUser;

public sealed record AssignRoleToUserCommand(Guid UserId, string RoleCode) : ICommand, IPermissionedCommand
{
    public string RequiredPermission => IdentityPermissions.RoleAssign;
}
