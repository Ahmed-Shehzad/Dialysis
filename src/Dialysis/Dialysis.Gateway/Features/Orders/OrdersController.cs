using Asp.Versioning;

using Dialysis.Domain.Entities;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Orders;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender) => _sender = sender;

    /// <summary>
    /// Create an order (ServiceRequest) - dialysis, medication, etc. Include X-Tenant-Id header.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PatientId) || string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { error = "PatientId and Code are required." });

        var result = await _sender.SendAsync(
            new CreateOrderCommand(
                request.PatientId,
                request.Code,
                request.Display,
                request.Intent,
                request.EncounterId,
                request.SessionId,
                request.ReasonText,
                request.RequesterId,
                request.Frequency,
                request.Category),
            cancellationToken);

        if (result.Error is not null)
            return BadRequest(new { error = result.Error });

        if (result.Order is null)
            return BadRequest();

        return CreatedAtAction(nameof(List), new { patientId = result.Order.PatientId.Value }, ToDto(result.Order));
    }

    /// <summary>
    /// List orders (CarePlan/ServiceRequest) for a patient. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string patientId,
        [FromQuery] string? status = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(patientId))
            return BadRequest(new { error = "patientId is required." });

        var orders = await _sender.SendAsync(
            new ListOrdersQuery(patientId, status, limit, offset),
            cancellationToken);

        return Ok(orders.Select(ToDto).ToList());
    }

    private static OrderDto ToDto(ServiceRequest o) => new(
        o.Id.ToString(),
        o.PatientId.Value,
        o.Code,
        o.Display,
        o.Status,
        o.Intent,
        o.EncounterId,
        o.SessionId,
        o.AuthoredOn,
        o.ReasonText,
        o.Frequency,
        o.Category);
}

public sealed record CreateOrderRequest(
    string PatientId,
    string Code,
    string? Display = null,
    string? Intent = "order",
    string? EncounterId = null,
    string? SessionId = null,
    string? ReasonText = null,
    string? RequesterId = null,
    string? Frequency = null,
    string? Category = null);

public sealed record OrderDto(
    string Id,
    string PatientId,
    string Code,
    string? Display,
    string Status,
    string? Intent,
    string? EncounterId,
    string? SessionId,
    DateTimeOffset? AuthoredOn,
    string? ReasonText,
    string? Frequency,
    string? Category);
