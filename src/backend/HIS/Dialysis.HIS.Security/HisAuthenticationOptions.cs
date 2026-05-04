using System.Security.Claims;

namespace Dialysis.HIS.Security;

/// <summary>Bound from configuration section <c>His:Authentication</c>. When <see cref="Authority"/> is empty, <see cref="Authorization.HttpContextCurrentUser"/> uses development permissions.</summary>
public sealed class HisAuthenticationOptions
{
    /// <summary>OIDC authority base URL. When null or whitespace, JWT is not registered and the dev user is used for <see cref="Authorization.ICurrentUser"/>.</summary>
    public string? Authority { get; set; }

    /// <summary>Optional API audience for access tokens.</summary>
    public string? Audience { get; set; }

    /// <summary>JWT claim type for HIS permission strings (must match <c>HisPermissions</c> values). Default: <c>his_permission</c>.</summary>
    public string PermissionClaimType { get; set; } = "his_permission";

    /// <summary>Claim type(s) carrying IdP role or group names. Matched against <see cref="RolePermissionMap"/> keys (ordinal ignore case). Default: <see cref="ClaimTypes.Role"/>.</summary>
    public string RoleClaimType { get; set; } = ClaimTypes.Role;

    /// <summary>Maps IdP role or group names to HIS permission strings (same values as <c>HisPermissions</c>). Merged with direct <see cref="PermissionClaimType"/> claims.</summary>
    public Dictionary<string, string[]> RolePermissionMap { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Optional explicit patient id claim for portal routes when <see cref="Authority"/> is set. Also accepts <c>sub</c> / nameidentifier matching route <c>patientId</c>.</summary>
    public string PatientPortalPatientIdClaimType { get; set; } = "his_patient_id";
}
