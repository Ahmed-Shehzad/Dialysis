using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Contracts.Integration;
using Dialysis.Identity.Provisioning.Domain;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.DeactivateUser;

public sealed class DeactivateUserCommandHandler(
    IUserAccountRepository users,
    ITransponderBus bus,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<DeactivateUserCommand>
{
    public async Task<Unit> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(request.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"User '{request.UserId}' not found.");

        if (user.Status == UserAccountStatus.Deactivated)
            return Unit.Value;

        user.Status = UserAccountStatus.Deactivated;
        users.Update(user);

        await bus.PublishAsync(
            new UserDeactivatedIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredOn: timeProvider.GetUtcNow().UtcDateTime,
                SchemaVersion: 1,
                UserId: user.Id,
                Subject: user.Subject),
            cancellationToken).ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
