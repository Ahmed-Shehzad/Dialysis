using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Contracts.Integration;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.RevokeRoleFromUser;

public sealed class RevokeRoleFromUserCommandHandler : ICommandHandler<RevokeRoleFromUserCommand>
{
    private readonly IUserAccountRepository _users;
    private readonly IRoleDefinitionRepository _roles;
    private readonly IRoleAssignmentRepository _assignments;
    private readonly ITransponderBus _bus;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public RevokeRoleFromUserCommandHandler(IUserAccountRepository users,
        IRoleDefinitionRepository roles,
        IRoleAssignmentRepository assignments,
        ITransponderBus bus,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users;
        _roles = roles;
        _assignments = assignments;
        _bus = bus;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
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

        await _bus.PublishAsync(
            new RoleRevokedIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredOn: _timeProvider.GetUtcNow().UtcDateTime,
                SchemaVersion: 1,
                UserId: user.Id,
                Subject: user.Subject,
                RoleCode: role.Code),
            cancellationToken).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
