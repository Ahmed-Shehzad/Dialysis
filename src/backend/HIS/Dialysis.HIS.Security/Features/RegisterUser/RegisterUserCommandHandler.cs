using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Security.Audit;
using Dialysis.HIS.Security.Domain;
using Dialysis.HIS.Security.Ports;

namespace Dialysis.HIS.Security.Features.RegisterUser;

public sealed class RegisterUserCommandHandler(
    IUserDirectoryRepository users,
    IUnitOfWork unitOfWork,
    IAuditTrail audit)
    : ICommandHandler<RegisterUserCommand>
{
    public async Task<Unit> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await users.FindByUserNameAsync(request.UserName, cancellationToken).ConfigureAwait(false) is not null)
            throw new InvalidOperationException($"User '{request.UserName}' already exists.");

        var user = new HisUserAccount
        {
            Id = Guid.CreateVersion7(),
            UserName = request.UserName,
            PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.Password)),
            CreatedAtUtc = DateTime.UtcNow,
        };

        users.AddUser(user);

        foreach (var code in request.RoleCodes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var role = await users.FindRoleByCodeAsync(code, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Unknown role code '{code}'.");
            users.AttachRole(user, role);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await audit.WriteAsync(
            "his.security.user.registered",
            user.Id.ToString(),
            request.UserName,
            cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
