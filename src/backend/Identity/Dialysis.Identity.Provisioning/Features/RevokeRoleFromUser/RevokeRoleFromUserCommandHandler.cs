using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.RevokeRoleFromUser;

public sealed class RevokeRoleFromUserCommandHandler : ICommandHandler<RevokeRoleFromUserCommand, Unit>
{
    private readonly IUserAccountRepository _users;
    private readonly IRoleDefinitionRepository _roles;
    private readonly IRoleAssignmentRepository _assignments;
    private readonly IUnitOfWork _unitOfWork;
    public RevokeRoleFromUserCommandHandler(IUserAccountRepository users,
        IRoleDefinitionRepository roles,
        IRoleAssignmentRepository assignments,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _roles = roles;
        _assignments = assignments;
        _unitOfWork = unitOfWork;
    }
    public async Task<Unit> HandleAsync(RevokeRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetAsync(request.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"User '{request.UserId}' not found.");
        var role = await _roles.FindByCodeAsync(request.RoleCode, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Role '{request.RoleCode}' not defined.");

        var assignment = await _assignments.FindAsync(user.Id, role.Id, cancellationToken).ConfigureAwait(false);
        if (assignment is null)
            return Unit.Value;

        _assignments.Remove(assignment);

        // The (tracked) user aggregate raises RoleRevoked; the SaveChanges interceptor drains it
        // into the Transponder outbox atomically with the assignment removal.
        user.RecordRoleRevoked(role.Code);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
