using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.Lab.Contracts;
using Dialysis.Lab.Orders.Features.GetLabOrderById;
using Dialysis.Lab.Orders.Features.ListLabOrdersByPatient;
using Dialysis.Lab.Orders.Features.PlaceLabOrder;
using Dialysis.Module.Contracts.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Lab.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/lab/orders")]
public sealed class LabOrdersController : ControllerBase
{
    private readonly ICqrsGateway _gateway;
    private readonly ICurrentUser _currentUser;
    public LabOrdersController(ICqrsGateway gateway, ICurrentUser currentUser)
    {
        _gateway = gateway;
        _currentUser = currentUser;
    }

    /// <summary>Places a lab order. The placing clinician is taken from the authenticated subject.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> PlaceAsync([FromBody] PlaceLabOrderRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var placedBy = _currentUser.UserId ?? User.Identity?.Name ?? "clinician";
        var id = await _gateway
            .SendCommandAsync<PlaceLabOrderCommand, Guid>(
                new PlaceLabOrderCommand(body.PatientId, body.Tests, body.Priority, body.Specimen, placedBy),
                cancellationToken)
            .ConfigureAwait(false);
        return Created($"/api/v1.0/lab/orders/{id}", new { id });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LabOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _gateway
            .SendQueryAsync<GetLabOrderByIdQuery, LabOrderDto?>(new GetLabOrderByIdQuery(id), cancellationToken)
            .ConfigureAwait(false);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet("by-patient/{patientId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<LabOrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByPatientAsync(
        Guid patientId, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var rows = await _gateway
            .SendQueryAsync<ListLabOrdersByPatientQuery, IReadOnlyList<LabOrderSummaryDto>>(
                new ListLabOrdersByPatientQuery(patientId, take), cancellationToken)
            .ConfigureAwait(false);
        return Ok(rows);
    }

    /// <summary>Order-placement request body. PlacedBy is derived server-side from the authenticated user.</summary>
    public sealed record PlaceLabOrderRequest(
        Guid PatientId,
        IReadOnlyList<LabTestRequestContract> Tests,
        LabOrderPriority Priority,
        string? Specimen);
}
