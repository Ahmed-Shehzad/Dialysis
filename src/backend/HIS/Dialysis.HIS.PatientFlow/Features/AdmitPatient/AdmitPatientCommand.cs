using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.PatientFlow.Features.AdmitPatient;

public sealed record AdmitPatientCommand(
    Guid PatientId,
    string WardCode) : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.PatientFlowAdmit;
}
