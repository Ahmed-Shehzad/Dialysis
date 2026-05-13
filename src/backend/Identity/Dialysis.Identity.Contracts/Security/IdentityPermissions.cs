using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Identity.Contracts.Security;

/// <summary>
/// Closed permission set for the Identity module. Covers user provisioning, role management,
/// permission grants, and inspection of identity audit records.
/// </summary>
public static class IdentityPermissions
{
    public const string UserProvision = "identity.user.provision";
    public const string UserDeactivate = "identity.user.deactivate";
    public const string UserRead = "identity.user.read";
    public const string RoleDefine = "identity.role.define";
    public const string RoleAssign = "identity.role.assign";
    public const string RoleRevoke = "identity.role.revoke";
    public const string RoleRead = "identity.role.read";
    public const string PermissionGrant = "identity.permission.grant";
    public const string PermissionRevoke = "identity.permission.revoke";
    public const string AuditRead = "identity.audit.read";

    public static IReadOnlyList<string> All { get; } =
    [
        UserProvision, UserDeactivate, UserRead,
        RoleDefine, RoleAssign, RoleRevoke, RoleRead,
        PermissionGrant, PermissionRevoke,
        AuditRead,
    ];
}

/// <summary>Module-hosting catalog binding for <see cref="IdentityPermissions"/>.</summary>
public sealed class IdentityPermissionCatalog : IModulePermissionCatalog
{
    public string ModuleSlug => "identity";

    public IReadOnlyCollection<string> All => IdentityPermissions.All;
}
