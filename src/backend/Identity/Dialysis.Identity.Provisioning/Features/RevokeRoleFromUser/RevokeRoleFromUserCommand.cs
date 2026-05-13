using Dialysis.CQRS.Commands;
using Dialysis.Identity.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Identity.Provisioning.Features.RevokeRoleFromUser;

public sealed record RevokeRoleFromUserCommand(Guid UserId, string RoleCode) : ICommand, IPermissionedCommand
{
    public string RequiredPermission => IdentityPermissions.RoleRevoke;
}
