using Dialysis.CQRS.Commands;
using Dialysis.Identity.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Identity.Provisioning.Features.DefineRole;

public sealed record DefineRoleCommand : ICommand<Guid>, IPermissionedCommand
{
    public DefineRoleCommand(string Code, string DisplayName, IReadOnlyList<string> Permissions)
    {
        this.Code = Code;
        this.DisplayName = DisplayName;
        this.Permissions = Permissions;
    }
    public string RequiredPermission => IdentityPermissions.RoleDefine;
    public string Code { get; init; }
    public string DisplayName { get; init; }
    public IReadOnlyList<string> Permissions { get; init; }
    public void Deconstruct(out string Code, out string DisplayName, out IReadOnlyList<string> Permissions)
    {
        Code = this.Code;
        DisplayName = this.DisplayName;
        Permissions = this.Permissions;
    }
}
