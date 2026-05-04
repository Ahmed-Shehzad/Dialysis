using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.PostOrganizationalCommunication;

public sealed record PostOrganizationalCommunicationCommand(string ThreadCode, string Subject, string Body)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
}
