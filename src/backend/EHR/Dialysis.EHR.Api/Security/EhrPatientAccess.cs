using System.Security.Claims;

namespace Dialysis.EHR.Api.Security;

/// <summary>
/// Resolves and verifies the caller's patient identity for EHR patient self-access (portal routes).
/// The patient id is carried as the <c>his_patient_id</c> claim — the platform's patient identity
/// claim — falling back to <c>sub</c>. Mirrors the HIE <c>HiePatientAccess</c> helper.
/// </summary>
/// <remarks>
/// Row-level scoping lives here in the controller, not in the CQRS authorization pipeline (which only
/// checks permission strings). When no OIDC <c>Authority</c> is configured the host runs in dev mode
/// with no authentication at all, so <see cref="IsSelf"/> bypasses the check — the synchronous API and
/// its permission gating still apply, and every deployed environment sets an Authority.
/// </remarks>
public static class EhrPatientAccess
{
    /// <summary>The platform patient identity claim.</summary>
    public const string PatientIdClaim = "his_patient_id";

    /// <summary>The caller's patient id from the identity claim (his_patient_id, then sub), or null.</summary>
    public static string? PatientId(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var fromClaim = user.FindFirst(PatientIdClaim)?.Value;
        return string.IsNullOrWhiteSpace(fromClaim) ? user.FindFirst("sub")?.Value : fromClaim;
    }

    /// <summary>
    /// True when the caller may act as <paramref name="patientId"/>: always in dev (no
    /// <paramref name="authorityConfigured"/>), otherwise only when the caller's patient claim matches.
    /// </summary>
    public static bool IsSelf(ClaimsPrincipal user, Guid patientId, bool authorityConfigured)
    {
        if (!authorityConfigured)
            return true;

        var id = PatientId(user);
        if (string.IsNullOrWhiteSpace(id))
            return false;

        return Guid.TryParse(id, out var claimPatientId)
            ? claimPatientId == patientId
            : string.Equals(id, patientId.ToString(), StringComparison.Ordinal);
    }
}
