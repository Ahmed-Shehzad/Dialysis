using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.UpdateQualityWorkflowTaskStatus;

public sealed record UpdateQualityWorkflowTaskStatusCommand(Guid TaskId, string NewStatusCode)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
}
