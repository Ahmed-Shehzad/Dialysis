using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Operations.Features.AssignStaffRole;

public sealed class AssignStaffPrimaryRoleCommandHandler : ICommandHandler<AssignStaffPrimaryRoleCommand>
{
    private readonly IStaffRepository _staff;
    private readonly IUnitOfWork _unitOfWork;
    public AssignStaffPrimaryRoleCommandHandler(IStaffRepository staff, IUnitOfWork unitOfWork)
    {
        _staff = staff;
        _unitOfWork = unitOfWork;
    }
    public async Task<Unit> HandleAsync(AssignStaffPrimaryRoleCommand request, CancellationToken cancellationToken)
    {
        var member = await _staff.GetAsync(request.StaffMemberId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Staff member not found.");

        member.AssignPrimaryRole(request.RoleCode);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
