using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.ClearPatientAlert;

public sealed record ClearPatientAlertCommand(Guid AlertId) : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
}
