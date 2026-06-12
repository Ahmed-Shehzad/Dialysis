using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.DeactivateUser;

public sealed class DeactivateUserCommandHandler : ICommandHandler<DeactivateUserCommand, Unit>
{
    private readonly IUserAccountRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    public DeactivateUserCommandHandler(IUserAccountRepository users,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }
    public async Task<Unit> HandleAsync(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetAsync(request.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"User '{request.UserId}' not found.");

        // The aggregate raises UserDeactivated (no-op when already deactivated); the SaveChanges
        // interceptor drains it into the Transponder outbox atomically with the state change.
        if (!user.Deactivate())
            return Unit.Value;

        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
