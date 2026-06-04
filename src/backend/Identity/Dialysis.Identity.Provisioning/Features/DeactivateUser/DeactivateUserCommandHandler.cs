using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Contracts.Integration;
using Dialysis.Identity.Provisioning.Domain;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.DeactivateUser;

public sealed class DeactivateUserCommandHandler : ICommandHandler<DeactivateUserCommand>
{
    private readonly IUserAccountRepository _users;
    private readonly ITransponderBus _bus;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public DeactivateUserCommandHandler(IUserAccountRepository users,
        ITransponderBus bus,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users;
        _bus = bus;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Unit> HandleAsync(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetAsync(request.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"User '{request.UserId}' not found.");

        if (user.Status == UserAccountStatus.Deactivated)
            return Unit.Value;

        user.Status = UserAccountStatus.Deactivated;
        _users.Update(user);

        await _bus.PublishAsync(
            new UserDeactivatedIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredOn: _timeProvider.GetUtcNow().UtcDateTime,
                SchemaVersion: 1,
                UserId: user.Id,
                Subject: user.Subject),
            cancellationToken).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
