using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Operations.Features.AssignStaffRole;

public sealed record AssignStaffPrimaryRoleCommand(Guid StaffMemberId, string RoleCode)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.StaffAssign;
}
