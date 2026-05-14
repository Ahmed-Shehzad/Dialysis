using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.Medication.Features.PlaceMedicationOrder;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/medication")]
public sealed class MedicationController(ICqrsGateway gateway) : HisHateoasControllerBase
{
    [HttpPost("orders")]
    [ProducesResponseType(typeof(ResourceEnvelope<PlaceMedicationOrderResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> PlaceOrderAsync(
        [FromBody] PlaceMedicationOrderCommand command,
        CancellationToken cancellationToken)
    {
        var id = await gateway.SendCommandAsync<PlaceMedicationOrderCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource(
            $"/api/v{ApiVersionSegment}/medication/orders/{id}",
            new PlaceMedicationOrderResponse(id));
    }

    public sealed record PlaceMedicationOrderResponse(Guid Id);
}
