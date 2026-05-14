using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.Scheduling.Features.BookAppointment;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/scheduling")]
public sealed class SchedulingController(ICqrsGateway gateway) : HisHateoasControllerBase
{
    [HttpPost("appointments")]
    [ProducesResponseType(typeof(ResourceEnvelope<BookAppointmentResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> BookAppointmentAsync(
        [FromBody] BookAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        var id = await gateway.SendCommandAsync<BookAppointmentCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource(
            $"/api/v{ApiVersionSegment}/scheduling/appointments/{id}",
            new BookAppointmentResponse(id));
    }

    public sealed record BookAppointmentResponse(Guid Id);
}
