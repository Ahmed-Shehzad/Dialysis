using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.PatientFlow.Features.AssignChair;

public sealed record AssignChairCommand : ICommand<Guid>, IPermissionedCommand
{
    public AssignChairCommand(Guid EntryId, string Chair)
    {
        this.EntryId = EntryId;
        this.Chair = Chair;
    }
    public string RequiredPermission => HisPermissions.PatientFlowQueueManage;
    public Guid EntryId { get; init; }
    public string Chair { get; init; }
    public void Deconstruct(out Guid EntryId, out string Chair)
    {
        EntryId = this.EntryId;
        Chair = this.Chair;
    }
}
