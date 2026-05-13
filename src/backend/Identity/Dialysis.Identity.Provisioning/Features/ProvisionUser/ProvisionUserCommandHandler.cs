using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Contracts.Integration;
using Dialysis.Identity.Provisioning.Domain;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.ProvisionUser;

public sealed class ProvisionUserCommandHandler(
    IUserAccountRepository users,
    ITransponderBus bus,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<ProvisionUserCommand, Guid>
{
    public async Task<Guid> Handle(ProvisionUserCommand request, CancellationToken cancellationToken)
    {
        if (await users.FindBySubjectAsync(request.Subject, cancellationToken).ConfigureAwait(false) is { } existing)
            return existing.Id;

        var id = Guid.CreateVersion7();
        var user = new UserAccount
        {
            Id = id,
            Subject = request.Subject,
            DisplayName = request.DisplayName,
            Email = request.Email,
            Status = UserAccountStatus.Provisioned,
        };
        users.Add(user);

        await bus.PublishAsync(
            new UserRegisteredIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredOn: timeProvider.GetUtcNow().UtcDateTime,
                UserId: id,
                Subject: request.Subject,
                DisplayName: request.DisplayName,
                Email: request.Email),
            cancellationToken).ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
