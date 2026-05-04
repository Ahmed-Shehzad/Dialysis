using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Security.Audit;
using Dialysis.HIS.Security.Ports;

namespace Dialysis.HIS.Security.Features.AssignRole;

public sealed class AssignRoleCommandHandler(IUserDirectoryRepository users, IUnitOfWork unitOfWork, IAuditTrail audit)
    : ICommandHandler<AssignRoleCommand>
{
    public async Task<Unit> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await users.FindByUserNameAsync(request.UserName, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"User '{request.UserName}' not found.");

        var role = await users.FindRoleByCodeAsync(request.RoleCode, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Unknown role code '{request.RoleCode}'.");

        users.AttachRole(user, role);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await audit.WriteAsync(
            "his.security.role.assigned",
            user.Id.ToString(),
            request.RoleCode,
            cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
