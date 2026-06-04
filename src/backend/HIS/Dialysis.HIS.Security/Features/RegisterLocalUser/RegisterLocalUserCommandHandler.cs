using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Security.Domain;
using Dialysis.HIS.Security.Domain.ValueObjects;
using Dialysis.HIS.Security.Ports;

namespace Dialysis.HIS.Security.Features.RegisterLocalUser;

public sealed class RegisterLocalUserCommandHandler : ICommandHandler<RegisterLocalUserCommand, Guid>
{
    private readonly ILocalUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    public RegisterLocalUserCommandHandler(ILocalUserRepository users,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(RegisterLocalUserCommand request, CancellationToken cancellationToken)
    {
        var loginName = new LoginName(request.LoginName);

        if (await _users.LoginNameExistsAsync(loginName.Value, cancellationToken).ConfigureAwait(false))
            throw new DomainException($"LocalUser with login '{loginName.Value}' already exists.");

        var user = LocalUser.Register(loginName, request.DisplayName, DateTime.UtcNow);
        _users.Add(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return user.Id;
    }
}
