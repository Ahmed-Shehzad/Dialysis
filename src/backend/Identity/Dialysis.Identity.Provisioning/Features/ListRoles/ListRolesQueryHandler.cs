using Dialysis.CQRS.Queries;
using Dialysis.Identity.Provisioning.Ports;

namespace Dialysis.Identity.Provisioning.Features.ListRoles;

public sealed class ListRolesQueryHandler : IQueryHandler<ListRolesQuery, IReadOnlyList<RoleSummaryDto>>
{
    private readonly IRoleDefinitionRepository _roles;
    public ListRolesQueryHandler(IRoleDefinitionRepository roles) => _roles = roles;
    public async Task<IReadOnlyList<RoleSummaryDto>> HandleAsync(ListRolesQuery request, CancellationToken cancellationToken)
    {
        var defs = await _roles.ListAsync(cancellationToken).ConfigureAwait(false);
        return [.. defs.Select(r => new RoleSummaryDto(r.Id, r.Code, r.DisplayName, [.. r.Permissions]))];
    }
}
