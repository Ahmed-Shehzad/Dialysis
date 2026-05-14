using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Operations.Features.AssignStaffRole;

public sealed class AssignStaffPrimaryRoleCommandHandler(IStaffRepository staff, IUnitOfWork unitOfWork)
    : ICommandHandler<AssignStaffPrimaryRoleCommand>
{
    public async Task<Unit> Handle(AssignStaffPrimaryRoleCommand request, CancellationToken cancellationToken)
    {
        var member = await staff.GetAsync(request.StaffMemberId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Staff member not found.");

        member.AssignPrimaryRole(request.RoleCode);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
