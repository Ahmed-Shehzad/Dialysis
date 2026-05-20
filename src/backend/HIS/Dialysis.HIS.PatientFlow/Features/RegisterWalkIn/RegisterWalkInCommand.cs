using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.PatientFlow.Features.GetTodaysQueue;

namespace Dialysis.HIS.PatientFlow.Features.RegisterWalkIn;

public sealed record RegisterWalkInCommand(
    string PatientName,
    string Mrn,
    bool EligibilityVerified)
    : ICommand<PatientQueueEntryDto>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.PatientFlowQueueManage;
}
