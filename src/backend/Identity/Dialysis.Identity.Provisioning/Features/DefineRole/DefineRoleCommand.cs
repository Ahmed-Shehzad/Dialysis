using Dialysis.CQRS.Commands;
using Dialysis.Identity.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Identity.Provisioning.Features.DefineRole;

public sealed record DefineRoleCommand(string Code, string DisplayName, IReadOnlyList<string> Permissions)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => IdentityPermissions.RoleDefine;
}
