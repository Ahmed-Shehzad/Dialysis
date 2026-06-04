using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Operations.Features.AssignStaffRole;

public sealed record AssignStaffPrimaryRoleCommand : ICommand, IPermissionedCommand
{
    public AssignStaffPrimaryRoleCommand(Guid StaffMemberId, string RoleCode)
    {
        this.StaffMemberId = StaffMemberId;
        this.RoleCode = RoleCode;
    }
    public string RequiredPermission => HisPermissions.StaffAssign;
    public Guid StaffMemberId { get; init; }
    public string RoleCode { get; init; }
    public void Deconstruct(out Guid StaffMemberId, out string RoleCode)
    {
        StaffMemberId = this.StaffMemberId;
        RoleCode = this.RoleCode;
    }
}
