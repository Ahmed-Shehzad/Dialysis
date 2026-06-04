using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.PatientFlow.Features.GetTodaysQueue;

namespace Dialysis.HIS.PatientFlow.Features.RegisterWalkIn;

public sealed record RegisterWalkInCommand : ICommand<PatientQueueEntryDto>, IPermissionedCommand
{
    public RegisterWalkInCommand(string PatientName,
        string Mrn,
        bool EligibilityVerified)
    {
        this.PatientName = PatientName;
        this.Mrn = Mrn;
        this.EligibilityVerified = EligibilityVerified;
    }
    public string RequiredPermission => HisPermissions.PatientFlowQueueManage;
    public string PatientName { get; init; }
    public string Mrn { get; init; }
    public bool EligibilityVerified { get; init; }
    public void Deconstruct(out string PatientName, out string Mrn, out bool EligibilityVerified)
    {
        PatientName = this.PatientName;
        Mrn = this.Mrn;
        EligibilityVerified = this.EligibilityVerified;
    }
}
