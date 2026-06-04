using Dialysis.BuildingBlocks.Hipaa.Audit;
using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.PatientAccess.Features.GetPatientPortalSummary;

[PhiAccess(PhiAccessAction.Read, "Patient")]
public sealed record GetPatientPortalSummaryQuery : IQuery<PatientPortalSummaryDto>, IPermissionedCommand
{
    public GetPatientPortalSummaryQuery(Guid PatientId) => this.PatientId = PatientId;
    public string RequiredPermission => HisPermissions.PatientPortalRead;
    public Guid PatientId { get; init; }
    public void Deconstruct(out Guid patientId) => patientId = this.PatientId;
}
