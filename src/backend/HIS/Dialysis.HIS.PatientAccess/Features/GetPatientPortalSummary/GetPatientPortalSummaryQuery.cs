using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.PatientAccess.Features.GetPatientPortalSummary;

public sealed record GetPatientPortalSummaryQuery(Guid PatientId)
    : IQuery<PatientPortalSummaryDto>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.PatientPortalRead;
}
