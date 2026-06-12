using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Identity.Provisioning.Domain;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.ProvisionUser;

public sealed class ProvisionUserCommandHandler : ICommandHandler<ProvisionUserCommand, Guid>
{
    private readonly IUserAccountRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    public ProvisionUserCommandHandler(IUserAccountRepository users,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(ProvisionUserCommand request, CancellationToken cancellationToken)
    {
        if (await _users.FindBySubjectAsync(request.Subject, cancellationToken).ConfigureAwait(false) is { } existing)
            return existing.Id;

        // The aggregate raises UserRegistered; the SaveChanges interceptor drains it into the
        // Transponder outbox atomically with the row — no manual publishing here.
        var id = Guid.CreateVersion7();
        var user = UserAccount.Provision(id, request.Subject, request.DisplayName, request.Email);
        _users.Add(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
