using Dialysis.CQRS.Commands;
using Dialysis.Identity.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Identity.Provisioning.Features.ProvisionUser;

public sealed record ProvisionUserCommand(string Subject, string DisplayName, string? Email)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => IdentityPermissions.UserProvision;
}
