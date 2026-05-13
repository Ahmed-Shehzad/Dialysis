using System.Security.Claims;

namespace Dialysis.Module.Hosting.Authorization;

/// <summary>
/// Bound from configuration section <c>{ModuleSlug}:Authentication</c> (or generically <c>Module:Authentication</c>).
/// When <see cref="Authority"/> is empty, the host runs in dev mode and <see cref="HttpContextCurrentUser"/> grants
/// every permission declared by the module's <see cref="Contracts.Authorization.IModulePermissionCatalog"/>.
/// </summary>
public sealed class ModuleAuthenticationOptions
{
    /// <summary>OIDC authority base URL. When null or whitespace, JWT is not registered and a dev user is used.</summary>
    public string? Authority { get; set; }

    /// <summary>Optional API audience for access tokens.</summary>
    public string? Audience { get; set; }

    /// <summary>JWT claim type for module permission strings. Default: <c>dialysis_permission</c>.</summary>
    public string PermissionClaimType { get; set; } = "dialysis_permission";

    /// <summary>Claim type carrying IdP role or group names. Matched against <see cref="RolePermissionMap"/> (ordinal ignore case). Default: <see cref="ClaimTypes.Role"/>.</summary>
    public string RoleClaimType { get; set; } = ClaimTypes.Role;

    /// <summary>Maps IdP role or group names to module permission strings. Merged with direct permission-claim values.</summary>
    public Dictionary<string, string[]> RolePermissionMap { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// When true and <see cref="Authority"/> is unset, the host fails at startup outside Development —
    /// guards against accidentally running production with dev-wide permissions.
    /// </summary>
    public bool RequireAuthorityWhenNotDevelopment { get; set; }

    /// <summary>Identifier used for the dev user when <see cref="Authority"/> is unset. Default: <c>"dev-user"</c>.</summary>
    public string DevelopmentUserId { get; set; } = "dev-user";
}
