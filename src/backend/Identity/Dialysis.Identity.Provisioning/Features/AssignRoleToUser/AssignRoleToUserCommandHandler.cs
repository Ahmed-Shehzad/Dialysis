using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Contracts.Integration;
using Dialysis.Identity.Provisioning.Domain;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.AssignRoleToUser;

public sealed class AssignRoleToUserCommandHandler(
    IUserAccountRepository users,
    IRoleDefinitionRepository roles,
    IRoleAssignmentRepository assignments,
    ITransponderBus bus,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AssignRoleToUserCommand>
{
    public async Task<Unit> Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(request.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"User '{request.UserId}' not found.");
        if (user.Status == UserAccountStatus.Deactivated)
            throw new InvalidOperationException($"User '{request.UserId}' is deactivated.");

        var role = await roles.FindByCodeAsync(request.RoleCode, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Role '{request.RoleCode}' not defined.");

        if (await assignments.FindAsync(user.Id, role.Id, cancellationToken).ConfigureAwait(false) is not null)
            return Unit.Value;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        assignments.Add(new RoleAssignment
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            RoleId = role.Id,
            AssignedAtUtc = now,
        });

        await bus.PublishAsync(
            new RoleAssignedIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredOn: now,
                UserId: user.Id,
                Subject: user.Subject,
                RoleCode: role.Code,
                Permissions: role.Permissions.ToArray()),
            cancellationToken).ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
