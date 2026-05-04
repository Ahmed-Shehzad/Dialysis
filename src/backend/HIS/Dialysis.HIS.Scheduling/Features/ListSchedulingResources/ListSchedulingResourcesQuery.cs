using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Scheduling.Features.ListSchedulingResources;

public sealed record ListSchedulingResourcesQuery(string? KindCode)
    : IQuery<IReadOnlyList<SchedulingResourceDto>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.SchedulingResourcesRead;
}
