using Asp.Versioning;
using Dialysis.BuildingBlocks.DurableCommandBus;
using Dialysis.CQRS;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Features.RecordAllergy;
using Dialysis.EHR.PatientChart.Features.RecordVitalSign;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

/// <summary>
/// Clinical-chart writes (vitals + allergies). Both endpoints route through the durable
/// command bus when the per-command feature flag is on — matches the PDMS / HIS
/// opt-in shape from PR #140 + #141. Flags default off; flip when production traffic
/// patterns justify it.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/patient-chart")]
public sealed class PatientChartController(ICqrsGateway gateway) : ControllerBase
{
    [HttpPost("vitals")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RecordVitalSignAsync(
        [FromBody] RecordVitalSignRequest body,
        [FromServices] IConfiguration configuration,
        [FromServices] IDurableCommandBus durableCommandBus,
        [FromHeader(Name = "X-Command-Id")] Guid? commandId,
        CancellationToken cancellationToken)
    {
        var useDurable = configuration.GetValue("Ehr:DurableCommands:RecordVitalSign:Enabled", false);
        var readingId = commandId ?? Guid.CreateVersion7();
        var command = new RecordVitalSignCommand(
            body.PatientId, body.EncounterId, body.LoincCode, body.Display,
            body.Value, body.UnitCode, body.ObservedAtUtc, body.RecordedByProviderId,
            ReadingId: readingId);

        if (useDurable)
        {
            try
            {
                var acceptance = await durableCommandBus
                    .EnqueueAsync<RecordVitalSignCommand, Guid>(command, commandId: readingId, cancellationToken)
                    .ConfigureAwait(false);
                Response.Headers["Location"] = acceptance.StatusEndpoint;
                return Accepted(acceptance.StatusEndpoint, new
                {
                    commandId = acceptance.CommandId,
                    correlationId = acceptance.CorrelationId,
                    statusEndpoint = acceptance.StatusEndpoint,
                    readingId,
                });
            }
            catch (DurableCommandException)
            {
                Response.Headers.Append("Retry-After", "5");
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        var id = await gateway.SendCommandAsync<RecordVitalSignCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/patient-chart/vitals/{id}", new { id });
    }

    [HttpPost("allergies")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RecordAllergyAsync(
        [FromBody] RecordAllergyRequest body,
        [FromServices] IConfiguration configuration,
        [FromServices] IDurableCommandBus durableCommandBus,
        [FromHeader(Name = "X-Command-Id")] Guid? commandId,
        CancellationToken cancellationToken)
    {
        var useDurable = configuration.GetValue("Ehr:DurableCommands:RecordAllergy:Enabled", false);
        var allergyId = commandId ?? Guid.CreateVersion7();
        var command = new RecordAllergyCommand(
            body.PatientId, body.AllergenSystem, body.AllergenCode, body.AllergenDisplay,
            body.Severity, body.VerificationStatus, body.ReactionText, body.OnsetDate,
            AllergyId: allergyId);

        if (useDurable)
        {
            try
            {
                var acceptance = await durableCommandBus
                    .EnqueueAsync<RecordAllergyCommand, Guid>(command, commandId: allergyId, cancellationToken)
                    .ConfigureAwait(false);
                Response.Headers["Location"] = acceptance.StatusEndpoint;
                return Accepted(acceptance.StatusEndpoint, new
                {
                    commandId = acceptance.CommandId,
                    correlationId = acceptance.CorrelationId,
                    statusEndpoint = acceptance.StatusEndpoint,
                    allergyId,
                });
            }
            catch (DurableCommandException)
            {
                Response.Headers.Append("Retry-After", "5");
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        var id = await gateway.SendCommandAsync<RecordAllergyCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/patient-chart/allergies/{id}", new { id });
    }

    public sealed record RecordVitalSignRequest(
        Guid PatientId,
        Guid? EncounterId,
        string LoincCode,
        string? Display,
        decimal Value,
        string UnitCode,
        DateTime ObservedAtUtc,
        Guid? RecordedByProviderId);

    public sealed record RecordAllergyRequest(
        Guid PatientId,
        string AllergenSystem,
        string AllergenCode,
        string? AllergenDisplay,
        AllergySeverity Severity,
        AllergyVerificationStatus VerificationStatus,
        string? ReactionText,
        DateOnly? OnsetDate);
}
