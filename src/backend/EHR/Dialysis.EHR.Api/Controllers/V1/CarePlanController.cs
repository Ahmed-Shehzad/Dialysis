using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Features.CarePlanning;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

/// <summary>
/// Structured care plans + trackable goals — beyond the SOAP note's free-text Plan. Authored by
/// clinicians and surfaced read-only to patients in the portal.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/care-plans")]
public sealed class CarePlanController : ControllerBase
{
    private readonly ICqrsGateway _gateway;
    public CarePlanController(ICqrsGateway gateway) => _gateway = gateway;

    /// <summary>Returns the patient's active care plan (with goals), or 204 when there isn't one.</summary>
    [HttpGet("patients/{patientId:guid}/active")]
    [ProducesResponseType(typeof(CarePlanView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetActiveAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var view = await _gateway.SendQueryAsync<GetActiveCarePlanQuery, CarePlanView?>(
            new GetActiveCarePlanQuery(patientId), cancellationToken).ConfigureAwait(false);
        return view is null ? NoContent() : Ok(view);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateCarePlanRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await _gateway.SendCommandAsync<CreateCarePlanCommand, Guid>(
            new CreateCarePlanCommand(body.PatientId, body.Title, body.AuthoredByProviderId),
            cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/care-plans/{id}", new { id });
    }

    [HttpPost("{carePlanId:guid}/goals")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddGoalAsync(Guid carePlanId, [FromBody] AddGoalRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await _gateway.SendCommandAsync<AddCarePlanGoalCommand, Guid>(
            new AddCarePlanGoalCommand(carePlanId, body.Description, body.TargetMeasure),
            cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/care-plans/{carePlanId}/goals/{id}", new { id });
    }

    [HttpPost("{carePlanId:guid}/goals/{goalId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateGoalStatusAsync(
        Guid carePlanId, Guid goalId, [FromBody] UpdateGoalStatusRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        await _gateway.SendCommandAsync<UpdateCarePlanGoalStatusCommand, Guid>(
            new UpdateCarePlanGoalStatusCommand(carePlanId, goalId, body.Status), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{carePlanId:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CloseAsync(Guid carePlanId, [FromBody] CloseCarePlanRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        await _gateway.SendCommandAsync<CloseCarePlanCommand, Guid>(
            new CloseCarePlanCommand(carePlanId, body.Status), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>Create-care-plan request body.</summary>
    public sealed record CreateCarePlanRequest(Guid PatientId, string Title, Guid AuthoredByProviderId);

    /// <summary>Add-goal request body.</summary>
    public sealed record AddGoalRequest(string Description, string? TargetMeasure);

    /// <summary>Update-goal-status request body.</summary>
    public sealed record UpdateGoalStatusRequest(CarePlanGoalStatus Status);

    /// <summary>Close-care-plan request body.</summary>
    public sealed record CloseCarePlanRequest(CarePlanStatus Status);
}
