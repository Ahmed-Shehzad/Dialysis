using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.PatientFlow.Features.DischargePatient;

public sealed record DischargePatientCommand : ICommand<Unit>, IPermissionedCommand
{
    public DischargePatientCommand(Guid AdmissionId) => this.AdmissionId = AdmissionId;
    public string RequiredPermission => HisPermissions.PatientFlowDischarge;
    public Guid AdmissionId { get; init; }
    public void Deconstruct(out Guid AdmissionId) => AdmissionId = this.AdmissionId;
}
