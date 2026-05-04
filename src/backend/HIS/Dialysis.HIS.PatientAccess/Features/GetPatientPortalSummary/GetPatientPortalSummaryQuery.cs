using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.PatientAccess.Ports;

namespace Dialysis.HIS.PatientAccess.Features.GetPatientPortalSummary;

public sealed record GetPatientPortalSummaryQuery(Guid PatientId)
    : IQuery<PatientPortalSummaryDto?>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.PortalRead;
}
