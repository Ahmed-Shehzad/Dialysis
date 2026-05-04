using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.ListIntegrationOutboxRecent;

public sealed record ListIntegrationOutboxRecentQuery(int Take = 50)
    : IQuery<IReadOnlyList<IntegrationOutboxMetadataRow>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.DataShareRead;
}
