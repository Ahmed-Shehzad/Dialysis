using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Controllers;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.Scheduling.Features.BookAppointment;
using Dialysis.HIS.Scheduling.Features.ListSchedulingResources;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

/// <summary>RA: <em>Planning and scheduling</em> — appointments and resources (Tummers et al., 2021).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/scheduling")]
public sealed class SchedulingController(ICqrsGateway gateway) : HisHateoasControllerBase
{
    [HttpGet("resources")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<SchedulingResourceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListResources([FromQuery] string? kind, CancellationToken cancellationToken)
    {
        var list = await gateway
            .SendQueryAsync<ListSchedulingResourcesQuery, IReadOnlyList<SchedulingResourceDto>>(
                new ListSchedulingResourcesQuery(string.IsNullOrWhiteSpace(kind) ? null : kind),
                cancellationToken)
            .ConfigureAwait(false);
        return OkResource(list, LinkCapabilitiesIndex(), LinkTo("ra:book-appointment", $"/api/v{ApiVersionSegment}/scheduling/appointments", "POST"));
    }

    [HttpPost("appointments")]
    [ProducesResponseType(typeof(ResourceEnvelope<BookAppointmentResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentCommand command, CancellationToken cancellationToken)
    {
        var id = await gateway.SendCommandAsync<BookAppointmentCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource($"{Request.Path}/{id}", new BookAppointmentResponse(id), LinkCapabilitiesIndex());
    }

    public sealed record BookAppointmentResponse(Guid Id);
}
