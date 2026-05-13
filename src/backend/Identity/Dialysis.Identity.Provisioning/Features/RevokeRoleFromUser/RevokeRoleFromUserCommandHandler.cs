using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Contracts.Integration;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.RevokeRoleFromUser;

public sealed class RevokeRoleFromUserCommandHandler(
    IUserAccountRepository users,
    IRoleDefinitionRepository roles,
    IRoleAssignmentRepository assignments,
    ITransponderBus bus,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RevokeRoleFromUserCommand>
{
    public async Task<Unit> Handle(RevokeRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(request.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"User '{request.UserId}' not found.");
        var role = await roles.FindByCodeAsync(request.RoleCode, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Role '{request.RoleCode}' not defined.");

        var assignment = await assignments.FindAsync(user.Id, role.Id, cancellationToken).ConfigureAwait(false);
        if (assignment is null)
            return Unit.Value;

        assignments.Remove(assignment);

        await bus.PublishAsync(
            new RoleRevokedIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredOn: timeProvider.GetUtcNow().UtcDateTime,
                UserId: user.Id,
                Subject: user.Subject,
                RoleCode: role.Code),
            cancellationToken).ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
