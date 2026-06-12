using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Provisioning.Domain;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.AssignRoleToUser;

public sealed class AssignRoleToUserCommandHandler : ICommandHandler<AssignRoleToUserCommand, Unit>
{
    private readonly IUserAccountRepository _users;
    private readonly IRoleDefinitionRepository _roles;
    private readonly IRoleAssignmentRepository _assignments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public AssignRoleToUserCommandHandler(IUserAccountRepository users,
        IRoleDefinitionRepository roles,
        IRoleAssignmentRepository assignments,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users;
        _roles = roles;
        _assignments = assignments;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Unit> HandleAsync(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetAsync(request.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"User '{request.UserId}' not found.");
        if (user.Status == UserAccountStatus.Deactivated)
            throw new InvalidOperationException($"User '{request.UserId}' is deactivated.");

        var role = await _roles.FindByCodeAsync(request.RoleCode, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Role '{request.RoleCode}' not defined.");

        if (await _assignments.FindAsync(user.Id, role.Id, cancellationToken).ConfigureAwait(false) is not null)
            return Unit.Value;

        _assignments.Add(new RoleAssignment
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            RoleId = role.Id,
            AssignedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
        });

        // The (tracked) user aggregate raises RoleAssigned; the SaveChanges interceptor drains it
        // into the Transponder outbox atomically with the assignment row.
        user.RecordRoleAssigned(role.Code, role.Permissions);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
