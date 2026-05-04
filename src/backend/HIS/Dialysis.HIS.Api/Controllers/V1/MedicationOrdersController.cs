using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Controllers;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.Medication.Features.DiscontinueMedicationOrder;
using Dialysis.HIS.Medication.Features.PlaceMedicationOrder;
using Dialysis.HIS.Medication.Features.RecordMedicationAdministration;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

/// <summary>RA: <em>Medication management</em> — orders and administration (Tummers et al., 2021).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/medication")]
public sealed class MedicationOrdersController(ICqrsGateway gateway) : HisHateoasControllerBase
{
    [HttpPost("orders")]
    [ProducesResponseType(typeof(ResourceEnvelope<PlaceMedicationOrderResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceMedicationOrderCommand command, CancellationToken cancellationToken)
    {
        var id = await gateway.SendCommandAsync<PlaceMedicationOrderCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource($"{Request.Path}/{id}", new PlaceMedicationOrderResponse(id), LinkCapabilitiesIndex());
    }

    [HttpPost("orders/{orderId:guid}/discontinue")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Discontinue(Guid orderId, CancellationToken cancellationToken)
    {
        await gateway.SendCommandAsync<DiscontinueMedicationOrderCommand, Unit>(new DiscontinueMedicationOrderCommand(orderId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("orders/{orderId:guid}/administration")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordAdministration(Guid orderId, CancellationToken cancellationToken)
    {
        await gateway
            .SendCommandAsync<RecordMedicationAdministrationCommand, Unit>(new RecordMedicationAdministrationCommand(orderId), cancellationToken)
            .ConfigureAwait(false);
        return NoContent();
    }

    public sealed record PlaceMedicationOrderResponse(Guid Id);
}
