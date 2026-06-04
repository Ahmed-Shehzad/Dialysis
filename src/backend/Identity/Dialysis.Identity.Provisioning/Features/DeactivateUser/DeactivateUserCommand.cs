using Dialysis.CQRS.Commands;
using Dialysis.Identity.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Identity.Provisioning.Features.DeactivateUser;

public sealed record DeactivateUserCommand : ICommand, IPermissionedCommand
{
    public DeactivateUserCommand(Guid UserId) => this.UserId = UserId;
    public string RequiredPermission => IdentityPermissions.UserDeactivate;
    public Guid UserId { get; init; }
    public void Deconstruct(out Guid userId) => userId = this.UserId;
}
