using Dialysis.CQRS.Queries;
using Dialysis.Identity.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Identity.Provisioning.Features.ListUserPermissions;

public sealed record UserPermissionsDto
{
    public UserPermissionsDto(Guid UserId,
        string Subject,
        string DisplayName,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions)
    {
        this.UserId = UserId;
        this.Subject = Subject;
        this.DisplayName = DisplayName;
        this.Roles = Roles;
        this.Permissions = Permissions;
    }
    public Guid UserId { get; init; }
    public string Subject { get; init; }
    public string DisplayName { get; init; }
    public IReadOnlyList<string> Roles { get; init; }
    public IReadOnlyList<string> Permissions { get; init; }
    public void Deconstruct(out Guid userId, out string subject, out string displayName, out IReadOnlyList<string> roles, out IReadOnlyList<string> permissions)
    {
        userId = UserId;
        subject = Subject;
        displayName = DisplayName;
        roles = Roles;
        permissions = Permissions;
    }
}

public sealed record ListUserPermissionsQuery : IQuery<UserPermissionsDto?>, IPermissionedCommand
{
    public ListUserPermissionsQuery(Guid UserId) => this.UserId = UserId;
    public string RequiredPermission => IdentityPermissions.UserRead;
    public Guid UserId { get; init; }
    public void Deconstruct(out Guid userId) => userId = UserId;
}
