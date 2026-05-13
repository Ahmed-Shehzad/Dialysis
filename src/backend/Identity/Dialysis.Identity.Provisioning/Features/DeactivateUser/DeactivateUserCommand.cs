using Dialysis.CQRS.Commands;
using Dialysis.Identity.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Identity.Provisioning.Features.DeactivateUser;

public sealed record DeactivateUserCommand(Guid UserId) : ICommand, IPermissionedCommand
{
    public string RequiredPermission => IdentityPermissions.UserDeactivate;
}
