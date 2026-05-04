using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.ListResearchEducationActivities;

public sealed record ListResearchEducationActivitiesQuery
    : IQuery<IReadOnlyList<RaResearchEducationActivityRow>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCapabilitiesRead;
}
