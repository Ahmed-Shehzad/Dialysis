using Dialysis.CQRS.Commands;
using Dialysis.Identity.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Identity.Provisioning.Features.ProvisionUser;

public sealed record ProvisionUserCommand : ICommand<Guid>, IPermissionedCommand
{
    public ProvisionUserCommand(string Subject, string DisplayName, string? Email)
    {
        this.Subject = Subject;
        this.DisplayName = DisplayName;
        this.Email = Email;
    }
    public string RequiredPermission => IdentityPermissions.UserProvision;
    public string Subject { get; init; }
    public string DisplayName { get; init; }
    public string? Email { get; init; }
    public void Deconstruct(out string Subject, out string DisplayName, out string? Email)
    {
        Subject = this.Subject;
        DisplayName = this.DisplayName;
        Email = this.Email;
    }
}
