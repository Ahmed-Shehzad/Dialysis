using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.PatientFlow.Features.AdmitPatient;

public sealed record AdmitPatientCommand : ICommand<Guid>, IPermissionedCommand
{
    public AdmitPatientCommand(Guid PatientId,
        string WardCode)
    {
        this.PatientId = PatientId;
        this.WardCode = WardCode;
    }
    public string RequiredPermission => HisPermissions.PatientFlowAdmit;
    public Guid PatientId { get; init; }
    public string WardCode { get; init; }
    public void Deconstruct(out Guid PatientId, out string WardCode)
    {
        PatientId = this.PatientId;
        WardCode = this.WardCode;
    }
}
