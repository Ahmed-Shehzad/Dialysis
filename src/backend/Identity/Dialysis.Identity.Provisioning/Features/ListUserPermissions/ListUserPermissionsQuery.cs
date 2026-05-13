using Dialysis.CQRS.Queries;
using Dialysis.Identity.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Identity.Provisioning.Features.ListUserPermissions;

public sealed record UserPermissionsDto(
    Guid UserId,
    string Subject,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record ListUserPermissionsQuery(Guid UserId)
    : IQuery<UserPermissionsDto?>, IPermissionedCommand
{
    public string RequiredPermission => IdentityPermissions.UserRead;
}
