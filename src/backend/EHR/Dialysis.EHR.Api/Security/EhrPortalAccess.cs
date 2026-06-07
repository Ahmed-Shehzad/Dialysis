using System.Security.Claims;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.Module.Hosting.Authorization;
using Microsoft.Extensions.Options;

namespace Dialysis.EHR.Api.Security;

/// <summary>
/// Authorizes patient-portal self-access. Normally a caller may only act as their own patient
/// (<see cref="EhrPatientAccess.IsSelf"/> against the <c>his_patient_id</c> claim). In dev, where the
/// only seeded persona is a staff/developer super-user with no patient claim, a config-gated
/// <i>impersonation</i> path lets a clinician/staff caller act as any patient so the portal is
/// exercisable end-to-end.
/// </summary>
/// <remarks>
/// The impersonation path is doubly gated and never reachable in production:
/// <list type="number">
///   <item><c>Ehr:Portal:AllowStaffImpersonation</c> defaults to <c>false</c> and is set <c>true</c>
///   only by the Aspire AppHost in dev <b>run</b> mode — it is never emitted into the published
///   compose/k8s artifacts.</item>
///   <item>The caller must hold a clinician permission (<see cref="EhrPermissions.ChartRead"/>) that a
///   real portal patient never has — patients carry only <c>ehr.portal.*</c> permissions.</item>
/// </list>
/// </remarks>
public sealed class EhrPortalAccess
{
    private readonly bool _authorityConfigured;
    private readonly bool _allowStaffImpersonation;
    private readonly ICurrentUser _currentUser;

    /// <summary>Resolves the gate from auth options, the dev impersonation flag, and the current user.</summary>
    public EhrPortalAccess(
        IOptions<ModuleAuthenticationOptions> authOptions,
        IConfiguration configuration,
        ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(authOptions);
        ArgumentNullException.ThrowIfNull(configuration);
        _authorityConfigured = !string.IsNullOrWhiteSpace(authOptions.Value.Authority);
        _allowStaffImpersonation = configuration.GetValue("Ehr:Portal:AllowStaffImpersonation", false);
        _currentUser = currentUser;
    }

    /// <summary>
    /// True when <paramref name="user"/> may act as <paramref name="patientId"/>: their own patient
    /// identity, or — only when dev impersonation is enabled — any patient if they hold a staff
    /// clinician permission.
    /// </summary>
    public bool CanActAs(ClaimsPrincipal user, Guid patientId)
    {
        if (EhrPatientAccess.IsSelf(user, patientId, _authorityConfigured))
            return true;

        return _allowStaffImpersonation
            && _currentUser.Permissions.Contains(EhrPermissions.ChartRead, StringComparer.Ordinal);
    }
}
