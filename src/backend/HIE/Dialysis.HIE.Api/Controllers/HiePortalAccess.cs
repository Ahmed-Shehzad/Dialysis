using System.Security.Claims;
using Dialysis.HIE.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Api.Controllers;

/// <summary>
/// Authorizes HIE patient self-access (the Community Health Record under TEFCA Individual Access
/// Services). Normally the caller's <c>his_patient_id</c> claim must match the requested patient
/// (<see cref="HiePatientAccess.IsSelf"/>). In dev — where the only seeded persona is a staff/developer
/// super-user with no patient claim — a config-gated impersonation path lets a staff caller read any
/// patient's outside records so the portal is exercisable end-to-end.
/// </summary>
/// <remarks>
/// Doubly gated, never reachable in production: <c>Hie:Portal:AllowStaffImpersonation</c> defaults to
/// <c>false</c> and is set <c>true</c> only by the Aspire AppHost in dev <b>run</b> mode (never emitted
/// into published compose/k8s artifacts), and the caller must hold the operator permission
/// <see cref="HiePermissions.InboundReceive"/> that a real portal patient never has.
/// </remarks>
public sealed class HiePortalAccess
{
    private readonly bool _allowStaffImpersonation;
    private readonly ICurrentUser _currentUser;

    /// <summary>Resolves the gate from the dev impersonation flag and the current user.</summary>
    public HiePortalAccess(IConfiguration configuration, ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _allowStaffImpersonation = configuration.GetValue("Hie:Portal:AllowStaffImpersonation", false);
        _currentUser = currentUser;
    }

    /// <summary>
    /// True when <paramref name="user"/> may act as <paramref name="patientReference"/>: their own
    /// patient identity, or — only when dev impersonation is enabled — any patient if they hold an
    /// operator permission.
    /// </summary>
    public bool CanActAs(ClaimsPrincipal user, string patientReference)
    {
        if (HiePatientAccess.IsSelf(user, patientReference))
            return true;

        return _allowStaffImpersonation
            && _currentUser.Permissions.Contains(HiePermissions.InboundReceive, StringComparer.Ordinal);
    }
}
