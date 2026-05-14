using Dialysis.CQRS.Queries;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.ListRoles;

public sealed class ListRolesQueryHandler(IRoleDefinitionRepository roles)
    : IQueryHandler<ListRolesQuery, IReadOnlyList<RoleSummaryDto>>
{
    public async Task<IReadOnlyList<RoleSummaryDto>> HandleAsync(ListRolesQuery request, CancellationToken cancellationToken)
    {
        var defs = await roles.ListAsync(cancellationToken).ConfigureAwait(false);
        return defs
            .Select(r => new RoleSummaryDto(r.Id, r.Code, r.DisplayName, r.Permissions.ToArray()))
            .ToList();
    }
}
