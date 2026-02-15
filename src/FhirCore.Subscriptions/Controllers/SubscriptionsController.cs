using Asp.Versioning;
using FhirCore.Subscriptions.Features.Subscriptions;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FhirCore.Subscriptions.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/subscriptions")]
[Authorize(Policy = "Admin")]
public sealed class SubscriptionsController : ControllerBase
{
    private readonly ISender _sender;

    public SubscriptionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await _sender.SendAsync(new ListSubscriptionsQuery(), cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        var item = await _sender.SendAsync(new GetSubscriptionQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var entry = await _sender.SendAsync(
            new CreateSubscriptionCommand(request.Criteria, request.Endpoint, request.EndpointType),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = entry.Id }, entry);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var updated = await _sender.SendAsync(
            new UpdateSubscriptionCommand(id, request.Criteria, request.Endpoint, request.EndpointType),
            cancellationToken);
        return updated ? Ok(new SubscriptionEntry { Id = id, Criteria = request.Criteria, Endpoint = request.Endpoint, EndpointType = request.EndpointType }) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var removed = await _sender.SendAsync(new DeleteSubscriptionCommand(id), cancellationToken);
        return removed ? NoContent() : NotFound();
    }
}

public sealed record CreateSubscriptionRequest
{
    public required string Criteria { get; init; }
    public required string Endpoint { get; init; }
    public string? EndpointType { get; init; }
}

public sealed record UpdateSubscriptionRequest
{
    public required string Criteria { get; init; }
    public required string Endpoint { get; init; }
    public string? EndpointType { get; init; }
}
