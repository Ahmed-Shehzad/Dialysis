using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.UpdateQualityWorkflowTaskStatus;

public sealed record UpdateQualityWorkflowTaskStatusCommand : ICommand, IPermissionedCommand
{
    public UpdateQualityWorkflowTaskStatusCommand(Guid TaskId, string NewStatusCode)
    {
        this.TaskId = TaskId;
        this.NewStatusCode = NewStatusCode;
    }
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
    public Guid TaskId { get; init; }
    public string NewStatusCode { get; init; }
    public void Deconstruct(out Guid TaskId, out string NewStatusCode)
    {
        TaskId = this.TaskId;
        NewStatusCode = this.NewStatusCode;
    }
}
