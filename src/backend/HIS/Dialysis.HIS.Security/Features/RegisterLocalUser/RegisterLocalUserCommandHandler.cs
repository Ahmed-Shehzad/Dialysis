using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Security.Domain;
using Dialysis.HIS.Security.Domain.ValueObjects;
using Dialysis.HIS.Security.Ports;

namespace Dialysis.HIS.Security.Features.RegisterLocalUser;

public sealed class RegisterLocalUserCommandHandler(
    ILocalUserRepository users,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterLocalUserCommand, Guid>
{
    public async Task<Guid> HandleAsync(RegisterLocalUserCommand request, CancellationToken cancellationToken)
    {
        var loginName = new LoginName(request.LoginName);

        if (await users.LoginNameExistsAsync(loginName.Value, cancellationToken).ConfigureAwait(false))
            throw new DomainException($"LocalUser with login '{loginName.Value}' already exists.");

        var user = LocalUser.Register(loginName, request.DisplayName, DateTime.UtcNow);
        users.Add(user);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return user.Id;
    }
}
