using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.ListIntegrationOutboxRecent;

public sealed record ListIntegrationOutboxRecentQuery : IQuery<IReadOnlyList<IntegrationOutboxMetadataRow>>, IPermissionedCommand
{
    public ListIntegrationOutboxRecentQuery(int Take = 50) => this.Take = Take;
    public string RequiredPermission => HisPermissions.DataShareRead;
    public int Take { get; init; }
    public void Deconstruct(out int take) => take = Take;
}
