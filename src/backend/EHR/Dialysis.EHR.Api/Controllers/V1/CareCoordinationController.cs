using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.EHR.Integration.Features.MarkHospitalEventFollowedUp;
using Dialysis.EHR.Integration.Ports;
using Dialysis.EHR.Integration.ReadModels;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

/// <summary>
/// Care-coordination surface: the facility-wide hospital-event follow-up worklist (admit / discharge /
/// outside-org encounter) and the per-patient chart card. Reads go direct to the read-model repository
/// (like <c>BillingController</c>); the follow-up mutation goes through CQRS for permission enforcement.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/care-coordination")]
public sealed class CareCoordinationController : ControllerBase
{
    private readonly IHospitalEventRepository _events;
    private readonly ICqrsGateway _gateway;
    public CareCoordinationController(IHospitalEventRepository events, ICqrsGateway gateway)
    {
        _events = events;
        _gateway = gateway;
    }

    /// <summary>Facility-wide worklist of hospital events still needing follow-up (most-recent first).</summary>
    [HttpGet("worklist/needs-follow-up")]
    [ProducesResponseType(typeof(IReadOnlyList<HospitalEventRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListNeedsFollowUpAsync([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        var rows = await _events.ListNeedsFollowUpAsync(take, cancellationToken).ConfigureAwait(false);
        return Ok(rows.Select(ToRow).ToArray());
    }

    /// <summary>A patient's hospital events (admissions, discharges, outside-org encounters) for the chart card.</summary>
    [HttpGet("patients/{patientId:guid}/hospital-events")]
    [ProducesResponseType(typeof(IReadOnlyList<HospitalEventRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListForPatientAsync(Guid patientId, [FromQuery] int take = 25, CancellationToken cancellationToken = default)
    {
        var rows = await _events.ListForPatientAsync(patientId, take, cancellationToken).ConfigureAwait(false);
        return Ok(rows.Select(ToRow).ToArray());
    }

    /// <summary>Marks a hospital event as followed-up so it drops off the worklist.</summary>
    [HttpPost("hospital-events/{id:guid}/follow-up")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkFollowedUpAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _gateway.SendCommandAsync<MarkHospitalEventFollowedUpCommand, Unit>(
                new MarkHospitalEventFollowedUpCommand(id), cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private static HospitalEventRow ToRow(HospitalEvent e) => new(
        e.Id, e.PatientId, e.Kind.ToString(), e.Source, e.OccurredAtUtc, e.Detail, e.ExternalPatientRef, e.FollowedUp);

    /// <summary>A hospital-event worklist / chart row.</summary>
    public sealed record HospitalEventRow(
        Guid Id,
        Guid? PatientId,
        string Kind,
        string Source,
        DateTime OccurredAtUtc,
        string? Detail,
        string? ExternalPatientRef,
        bool FollowedUp);
}
