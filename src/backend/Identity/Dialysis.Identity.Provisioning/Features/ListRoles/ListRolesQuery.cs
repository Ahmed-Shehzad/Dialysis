using Dialysis.CQRS.Queries;
using Dialysis.Identity.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Identity.Provisioning.Features.ListRoles;

public sealed record RoleSummaryDto(Guid Id, string Code, string DisplayName, IReadOnlyList<string> Permissions);

public sealed record ListRolesQuery : IQuery<IReadOnlyList<RoleSummaryDto>>, IPermissionedCommand
{
    public string RequiredPermission => IdentityPermissions.RoleRead;
}
