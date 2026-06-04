using Dialysis.CQRS.Commands;
using Dialysis.Identity.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Identity.Provisioning.Features.AssignRoleToUser;

public sealed record AssignRoleToUserCommand : ICommand, IPermissionedCommand
{
    public AssignRoleToUserCommand(Guid UserId, string RoleCode)
    {
        this.UserId = UserId;
        this.RoleCode = RoleCode;
    }
    public string RequiredPermission => IdentityPermissions.RoleAssign;
    public Guid UserId { get; init; }
    public string RoleCode { get; init; }
    public void Deconstruct(out Guid UserId, out string RoleCode)
    {
        UserId = this.UserId;
        RoleCode = this.RoleCode;
    }
}
