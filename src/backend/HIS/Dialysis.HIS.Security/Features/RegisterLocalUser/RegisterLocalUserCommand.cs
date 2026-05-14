using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Security.Features.RegisterLocalUser;

public sealed record RegisterLocalUserCommand(
    string LoginName,
    string DisplayName) : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.SecurityManage;
}
