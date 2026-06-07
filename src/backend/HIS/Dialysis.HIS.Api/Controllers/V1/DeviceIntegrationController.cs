using Asp.Versioning;
using Dialysis.BuildingBlocks.DurableCommandBus;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.Integration.Features.IngestDeviceReading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dialysis.HIS.Api.Controllers.V1;

/// <summary>RA: <em>Patient monitoring</em> — device / telemetry ingest path (Tummers et al., 2021).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/integration")]
public sealed class DeviceIntegrationController : HisHateoasControllerBase
{
    private readonly ICqrsGateway _gateway;
    /// <summary>RA: <em>Patient monitoring</em> — device / telemetry ingest path (Tummers et al., 2021).</summary>
    public DeviceIntegrationController(ICqrsGateway gateway) => _gateway = gateway;
    [HttpPost("device-readings")]
    [EnableRateLimiting("DeviceIngest")]
    [ProducesResponseType(typeof(ResourceEnvelope<IngestDeviceReadingResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> IngestDeviceReadingAsync(
        [FromBody] IngestDeviceReadingRequest body,
        [FromServices] IConfiguration configuration,
        [FromServices] IDurableCommandBus durableCommandBus,
        [FromHeader(Name = "X-Command-Id")] Guid? commandId,
        CancellationToken cancellationToken)
    {
        // Feature flag — false (default) keeps the existing synchronous path; true routes
        // the write through the durable command bus and returns 202 with a poll URL.
        // Same shape as PDMS RecordReading; the underlying handler is unchanged.
        var useDurablePath = configuration.GetValue("His:DurableCommands:IngestDeviceReading:Enabled", false);
        var readingId = commandId ?? Guid.CreateVersion7();
        var commandWithId = new IngestDeviceReadingCommand(
            body.DeviceId,
            body.PatientId,
            body.PayloadJson,
            body.ExternalMessageId,
            ReadingId: readingId);

        if (useDurablePath)
        {
            try
            {
                var acceptance = await durableCommandBus
                    .EnqueueAsync<IngestDeviceReadingCommand, Guid>(commandWithId, commandId: readingId, cancellationToken)
                    .ConfigureAwait(false);
                Response.Headers["Location"] = acceptance.StatusEndpoint;
                return Accepted(acceptance.StatusEndpoint, new
                {
                    commandId = acceptance.CommandId,
                    correlationId = acceptance.CorrelationId,
                    statusEndpoint = acceptance.StatusEndpoint,
                    // The reading id is deterministic from CommandId (see the handler),
                    // so the caller knows the new device-reading row's id without polling.
                    readingId,
                });
            }
            catch (DurableCommandException)
            {
                Response.Headers.Append("Retry-After", "5");
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        try
        {
            var id = await _gateway.SendCommandAsync<IngestDeviceReadingCommand, Guid>(commandWithId, cancellationToken).ConfigureAwait(false);
            return CreatedResource($"{Request.Path}/{id}", new IngestDeviceReadingResponse(id), LinkCapabilitiesIndex());
        }
        catch (Dialysis.DomainDrivenDesign.Exceptions.DomainException ex)
        {
            // Registry governance rejected the reading (unknown/suspended/retired device, or a
            // patient-binding mismatch). Surface as a 400 rather than a 500.
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Request body for device-reading ingest. Deliberately does <em>not</em> carry the reading id:
    /// the controller derives it from the optional <c>X-Command-Id</c> header (or generates one) so a
    /// retry yields the same row. Keeping the id off the body also keeps the command's optional
    /// <c>Guid</c> parameter out of the OpenAPI schema surface — mirrors PDMS <c>RecordReadingRequest</c>.
    /// </summary>
    public sealed record IngestDeviceReadingRequest
    {
        /// <summary>Creates the request body.</summary>
        public IngestDeviceReadingRequest(string DeviceId,
            Guid PatientId,
            string PayloadJson,
            string? ExternalMessageId = null)
        {
            this.DeviceId = DeviceId;
            this.PatientId = PatientId;
            this.PayloadJson = PayloadJson;
            this.ExternalMessageId = ExternalMessageId;
        }
        /// <summary>External identifier of the source device.</summary>
        public string DeviceId { get; init; }
        /// <summary>Patient the reading belongs to.</summary>
        public Guid PatientId { get; init; }
        /// <summary>Raw device payload, serialized as JSON.</summary>
        public string PayloadJson { get; init; }
        /// <summary>Optional source-message id for idempotency / traceability.</summary>
        public string? ExternalMessageId { get; init; }
        /// <summary>Deconstructs the request body.</summary>
        public void Deconstruct(out string deviceId, out Guid patientId, out string payloadJson, out string? externalMessageId)
        {
            deviceId = this.DeviceId;
            patientId = this.PatientId;
            payloadJson = this.PayloadJson;
            externalMessageId = this.ExternalMessageId;
        }
    }

    public sealed record IngestDeviceReadingResponse
    {
        public IngestDeviceReadingResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = this.Id;
    }
}
