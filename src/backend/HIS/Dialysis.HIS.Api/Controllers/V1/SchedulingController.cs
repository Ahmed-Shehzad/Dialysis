using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.Scheduling.Features.BookAppointment;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/scheduling")]
public sealed class SchedulingController : HisHateoasControllerBase
{
    private readonly ICqrsGateway _gateway;
    public SchedulingController(ICqrsGateway gateway) => _gateway = gateway;
    [HttpPost("appointments")]
    [ProducesResponseType(typeof(ResourceEnvelope<BookAppointmentResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> BookAppointmentAsync(
        [FromBody] BookAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _gateway.SendCommandAsync<BookAppointmentCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource(
            $"/api/v{ApiVersionSegment}/scheduling/appointments/{id}",
            new BookAppointmentResponse(id));
    }

    public sealed record BookAppointmentResponse
    {
        public BookAppointmentResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = this.Id;
    }
}
