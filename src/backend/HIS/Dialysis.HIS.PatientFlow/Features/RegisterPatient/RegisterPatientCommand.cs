using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.PatientFlow.Features.RegisterPatient;

public sealed record RegisterPatientCommand(string MedicalRecordNumber)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.PatientRegister;
}
