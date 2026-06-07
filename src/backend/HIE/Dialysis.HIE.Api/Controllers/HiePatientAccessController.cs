using System.Security.Claims;
using Asp.Versioning;
using Dialysis.HIE.Api.Hateoas;
using Dialysis.HIE.Inbound.Insights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIE.Api.Controllers;

/// <summary>
/// Patient self-access to the Community Health Record under the TEFCA <c>IndividualAccessServices</c>
/// purpose. Unlike the operator <c>/hie/ops</c> insights endpoint (which requires
/// <c>hie.inbound.receive</c>), this route is authorized by the caller's own patient identity claim —
/// a patient can only ever retrieve their own consolidated outside records. The claim *is* the
/// authorization, so it reads the insights projection directly rather than via the operator-permissioned
/// query.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hie/patient-access")]
[Authorize]
public sealed class HiePatientAccessController : ControllerBase
{
    private readonly ExternalPatientInsightsBuilder _builder;
    private readonly HiePortalAccess _portalAccess;
    public HiePatientAccessController(ExternalPatientInsightsBuilder builder, HiePortalAccess portalAccess)
    {
        _builder = builder;
        _portalAccess = portalAccess;
    }

    /// <summary>
    /// The caller's own consolidated outside records. <paramref name="patientReference"/> must match
    /// the caller's patient identity claim, else <c>403</c>.
    /// </summary>
    [HttpGet("insights/patient/{patientReference}")]
    [ProducesResponseType(typeof(ResourceEnvelope<PatientInsightsSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyInsightsAsync(
        string patientReference,
        [FromQuery] int scan = 500,
        [FromQuery] int recentTake = 20,
        CancellationToken cancellationToken = default)
    {
        if (!_portalAccess.CanActAs(User, patientReference))
            return Forbid();

        var summary = await _builder.BuildAsync(patientReference, scan, recentTake, cancellationToken).ConfigureAwait(false);
        return Ok(new ResourceEnvelope<PatientInsightsSummary>(
            summary,
            [new LinkDto("self", $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}", "GET")]));
    }
}

/// <summary>
/// Resolves and verifies the caller's patient identity for HIE patient self-access. The patient id is
/// carried as the <c>his_patient_id</c> claim (the platform's patient identity claim), falling back to
/// <c>sub</c>.
/// </summary>
public static class HiePatientAccess
{
    public const string PatientIdClaim = "his_patient_id";

    /// <summary>The caller's patient id from the identity claim, or null when absent.</summary>
    public static string? PatientId(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var fromClaim = user.FindFirst(PatientIdClaim)?.Value;
        return string.IsNullOrWhiteSpace(fromClaim) ? user.FindFirst("sub")?.Value : fromClaim;
    }

    /// <summary>True only when the caller's patient claim matches <paramref name="patientReference"/>.</summary>
    public static bool IsSelf(ClaimsPrincipal user, string patientReference) =>
        !string.IsNullOrWhiteSpace(patientReference)
        && PatientId(user) is { Length: > 0 } id
        && string.Equals(id, patientReference, StringComparison.Ordinal);
}
