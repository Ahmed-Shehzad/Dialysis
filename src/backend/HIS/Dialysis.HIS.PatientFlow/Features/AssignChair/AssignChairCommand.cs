using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.PatientFlow.Features.AssignChair;

public sealed record AssignChairCommand(Guid EntryId, string Chair)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.PatientFlowQueueManage;
}
