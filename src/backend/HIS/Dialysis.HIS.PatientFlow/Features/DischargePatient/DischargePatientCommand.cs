using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.PatientFlow.Features.DischargePatient;

public sealed record DischargePatientCommand(Guid PatientId)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.PatientDischarge;
}
