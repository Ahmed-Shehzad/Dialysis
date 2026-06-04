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
public sealed class PatientChartController : ControllerBase
{
    private readonly ICqrsGateway _gateway;
    /// <summary>
    /// Clinical-chart writes (vitals + allergies). Both endpoints route through the durable
    /// command bus when the per-command feature flag is on — matches the PDMS / HIS
    /// opt-in shape from PR #140 + #141. Flags default off; flip when production traffic
    /// patterns justify it.
    /// </summary>
    public PatientChartController(ICqrsGateway gateway) => _gateway = gateway;
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

        var id = await _gateway.SendCommandAsync<RecordVitalSignCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
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

        var id = await _gateway.SendCommandAsync<RecordAllergyCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/patient-chart/allergies/{id}", new { id });
    }

    public sealed record RecordVitalSignRequest
    {
        public RecordVitalSignRequest(Guid PatientId,
            Guid? EncounterId,
            string LoincCode,
            string? Display,
            decimal Value,
            string UnitCode,
            DateTime ObservedAtUtc,
            Guid? RecordedByProviderId)
        {
            this.PatientId = PatientId;
            this.EncounterId = EncounterId;
            this.LoincCode = LoincCode;
            this.Display = Display;
            this.Value = Value;
            this.UnitCode = UnitCode;
            this.ObservedAtUtc = ObservedAtUtc;
            this.RecordedByProviderId = RecordedByProviderId;
        }
        public Guid PatientId { get; init; }
        public Guid? EncounterId { get; init; }
        public string LoincCode { get; init; }
        public string? Display { get; init; }
        public decimal Value { get; init; }
        public string UnitCode { get; init; }
        public DateTime ObservedAtUtc { get; init; }
        public Guid? RecordedByProviderId { get; init; }
        public void Deconstruct(out Guid PatientId, out Guid? EncounterId, out string LoincCode, out string? Display, out decimal Value, out string UnitCode, out DateTime ObservedAtUtc, out Guid? RecordedByProviderId)
        {
            PatientId = this.PatientId;
            EncounterId = this.EncounterId;
            LoincCode = this.LoincCode;
            Display = this.Display;
            Value = this.Value;
            UnitCode = this.UnitCode;
            ObservedAtUtc = this.ObservedAtUtc;
            RecordedByProviderId = this.RecordedByProviderId;
        }
    }

    public sealed record RecordAllergyRequest
    {
        public RecordAllergyRequest(Guid PatientId,
            string AllergenSystem,
            string AllergenCode,
            string? AllergenDisplay,
            AllergySeverity Severity,
            AllergyVerificationStatus VerificationStatus,
            string? ReactionText,
            DateOnly? OnsetDate)
        {
            this.PatientId = PatientId;
            this.AllergenSystem = AllergenSystem;
            this.AllergenCode = AllergenCode;
            this.AllergenDisplay = AllergenDisplay;
            this.Severity = Severity;
            this.VerificationStatus = VerificationStatus;
            this.ReactionText = ReactionText;
            this.OnsetDate = OnsetDate;
        }
        public Guid PatientId { get; init; }
        public string AllergenSystem { get; init; }
        public string AllergenCode { get; init; }
        public string? AllergenDisplay { get; init; }
        public AllergySeverity Severity { get; init; }
        public AllergyVerificationStatus VerificationStatus { get; init; }
        public string? ReactionText { get; init; }
        public DateOnly? OnsetDate { get; init; }
        public void Deconstruct(out Guid PatientId, out string AllergenSystem, out string AllergenCode, out string? AllergenDisplay, out AllergySeverity Severity, out AllergyVerificationStatus VerificationStatus, out string? ReactionText, out DateOnly? OnsetDate)
        {
            PatientId = this.PatientId;
            AllergenSystem = this.AllergenSystem;
            AllergenCode = this.AllergenCode;
            AllergenDisplay = this.AllergenDisplay;
            Severity = this.Severity;
            VerificationStatus = this.VerificationStatus;
            ReactionText = this.ReactionText;
            OnsetDate = this.OnsetDate;
        }
    }
}
