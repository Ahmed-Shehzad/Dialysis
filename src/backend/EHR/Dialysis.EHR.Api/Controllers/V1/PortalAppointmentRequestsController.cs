using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.EHR.Api.Security;
using Dialysis.EHR.PatientPortal.Features.CancelAppointmentRequest;
using Dialysis.EHR.PatientPortal.Features.ListAppointmentRequests;
using Dialysis.EHR.PatientPortal.Features.RequestAppointment;
using Dialysis.EHR.PatientPortal.Features.ResolveAppointmentRequest;
using Dialysis.EHR.Scheduling.Features.BookAppointment;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

/// <summary>
/// Patient self-service appointment requests. Patients submit, list, and cancel their own requests
/// (gated by their patient identity claim); staff work the pending queue and approve (which books the
/// real appointment via the Scheduling slice, then links it) or decline.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/portal/appointment-requests")]
public sealed class PortalAppointmentRequestsController : ControllerBase
{
    private readonly ICqrsGateway _gateway;
    private readonly EhrPortalAccess _portalAccess;

    public PortalAppointmentRequestsController(ICqrsGateway gateway, EhrPortalAccess portalAccess)
    {
        _gateway = gateway;
        _portalAccess = portalAccess;
    }

    private bool IsSelf(Guid patientId) => _portalAccess.CanActAs(User, patientId);

    /// <summary>Patient submits an appointment request.</summary>
    [HttpPost("patients/{patientId:guid}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RequestAsync(
        Guid patientId, [FromBody] RequestBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (!IsSelf(patientId))
            return Forbid();

        Guid id;
        try
        {
            id = await _gateway.SendCommandAsync<RequestAppointmentCommand, Guid>(
                new RequestAppointmentCommand(patientId, body.ReasonText, body.EarliestPreferredUtc, body.LatestPreferredUtc),
                cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            // Invalid request window (past date, latest-before-earliest) — bad input, not a server fault.
            return BadRequest(new { error = ex.Message });
        }
        return Created($"/api/v1.0/portal/appointment-requests/patients/{patientId}", new { id });
    }

    /// <summary>Patient lists their own appointment requests.</summary>
    [HttpGet("patients/{patientId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentRequestView>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListMineAsync(Guid patientId, CancellationToken cancellationToken)
    {
        if (!IsSelf(patientId))
            return Forbid();
        var rows = await _gateway.SendQueryAsync<ListMyAppointmentRequestsQuery, IReadOnlyList<AppointmentRequestView>>(
            new ListMyAppointmentRequestsQuery(patientId), cancellationToken).ConfigureAwait(false);
        return Ok(rows);
    }

    /// <summary>Patient cancels their own still-pending request.</summary>
    [HttpPost("patients/{patientId:guid}/{requestId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CancelAsync(Guid patientId, Guid requestId, CancellationToken cancellationToken)
    {
        if (!IsSelf(patientId))
            return Forbid();
        try
        {
            await _gateway.SendCommandAsync<CancelAppointmentRequestCommand, Unit>(
                new CancelAppointmentRequestCommand(requestId), cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            // Request already resolved/cancelled — a state conflict, not a server fault.
            return Conflict(new { error = ex.Message });
        }
        return NoContent();
    }

    /// <summary>Staff worklist of still-pending requests.</summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentRequestView>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PendingAsync([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        var rows = await _gateway.SendQueryAsync<ListPendingAppointmentRequestsQuery, IReadOnlyList<AppointmentRequestView>>(
            new ListPendingAppointmentRequestsQuery(take), cancellationToken).ConfigureAwait(false);
        return Ok(rows);
    }

    /// <summary>
    /// Staff approve a request: first books the appointment (Scheduling slice), then links it to the
    /// request and notifies the patient. The two dispatches are not a single transaction — the booking
    /// is the durable commitment, the request transition is a follow-up.
    /// </summary>
    [HttpPost("{requestId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveAsync(
        Guid requestId, [FromBody] ApproveBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        Guid appointmentId;
        try
        {
            appointmentId = await _gateway.SendCommandAsync<BookAppointmentCommand, Guid>(
                new BookAppointmentCommand(
                    body.PatientId, body.ProviderId, body.StartUtc, body.EndUtc, body.EncounterClassCode, body.VisitReason),
                cancellationToken).ConfigureAwait(false);

            await _gateway.SendCommandAsync<ApproveAppointmentRequestCommand, Unit>(
                new ApproveAppointmentRequestCommand(requestId, appointmentId, body.StaffNote), cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            // e.g. the provider already has an overlapping appointment in this slot, or the request was
            // already resolved — a conflict with current scheduling state, not a server fault. Pick a
            // different slot/provider and retry.
            return Conflict(new { error = ex.Message });
        }

        return Ok(new { appointmentId });
    }

    /// <summary>Staff decline a request with a note.</summary>
    [HttpPost("{requestId:guid}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeclineAsync(
        Guid requestId, [FromBody] DeclineBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        try
        {
            await _gateway.SendCommandAsync<DeclineAppointmentRequestCommand, Unit>(
                new DeclineAppointmentRequestCommand(requestId, body.StaffNote), cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            // Request already resolved, or missing staff note — a state/input conflict, not a server fault.
            return Conflict(new { error = ex.Message });
        }
        return NoContent();
    }

    /// <summary>Patient appointment-request body.</summary>
    public sealed record RequestBody(string ReasonText, DateTime EarliestPreferredUtc, DateTime LatestPreferredUtc);

    /// <summary>Staff approve body — the chosen slot/provider plus the patient to book for.</summary>
    public sealed record ApproveBody(
        Guid PatientId,
        Guid ProviderId,
        DateTime StartUtc,
        DateTime EndUtc,
        string EncounterClassCode,
        string? VisitReason,
        string? StaffNote);

    /// <summary>Staff decline body.</summary>
    public sealed record DeclineBody(string StaffNote);
}
