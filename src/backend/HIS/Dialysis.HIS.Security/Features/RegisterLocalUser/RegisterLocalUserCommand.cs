using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Security.Features.RegisterLocalUser;

public sealed record RegisterLocalUserCommand : ICommand<Guid>, IPermissionedCommand
{
    public RegisterLocalUserCommand(string LoginName,
        string DisplayName)
    {
        this.LoginName = LoginName;
        this.DisplayName = DisplayName;
    }
    public string RequiredPermission => HisPermissions.SecurityManage;
    public string LoginName { get; init; }
    public string DisplayName { get; init; }
    public void Deconstruct(out string LoginName, out string DisplayName)
    {
        LoginName = this.LoginName;
        DisplayName = this.DisplayName;
    }
}
