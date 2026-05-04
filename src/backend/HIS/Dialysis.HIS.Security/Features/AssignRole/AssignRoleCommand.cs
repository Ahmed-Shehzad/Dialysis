using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Security.Features.AssignRole;

public sealed record AssignRoleCommand(string UserName, string RoleCode)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RoleAssign;
}
