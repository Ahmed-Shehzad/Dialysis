using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.Medication.Features.PlaceMedicationOrder;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/medication")]
public sealed class MedicationController : HisHateoasControllerBase
{
    private readonly ICqrsGateway _gateway;
    public MedicationController(ICqrsGateway gateway) => _gateway = gateway;
    [HttpPost("orders")]
    [ProducesResponseType(typeof(ResourceEnvelope<PlaceMedicationOrderResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> PlaceOrderAsync(
        [FromBody] PlaceMedicationOrderCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _gateway.SendCommandAsync<PlaceMedicationOrderCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource(
            $"/api/v{ApiVersionSegment}/medication/orders/{id}",
            new PlaceMedicationOrderResponse(id));
    }

    public sealed record PlaceMedicationOrderResponse
    {
        public PlaceMedicationOrderResponse(Guid Id) => this.Id = Id;
        public Guid Id { get; init; }
        public void Deconstruct(out Guid id) => id = this.Id;
    }
}
