using Dialysis.Fhir.Api.Subscriptions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Fhir.Api.Controllers;

/// <summary>
/// Internal endpoint for services to notify the subscription dispatcher when FHIR resources are created/updated.
/// Protected by FhirSubscription:NotifyApiKey (X-Subscription-Notify-ApiKey header).
/// </summary>
[ApiController]
[Route("api/fhir/subscription-notify")]
public sealed class SubscriptionNotifyController : ControllerBase
{
    private readonly SubscriptionDispatcher _dispatcher;
    private readonly IConfiguration _config;

    public SubscriptionNotifyController(SubscriptionDispatcher dispatcher, IConfiguration config)
    {
        _dispatcher = dispatcher;
        _config = config;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> NotifyAsync(
        [FromBody] SubscriptionNotifyRequest request,
        [FromHeader(Name = "X-Subscription-Notify-ApiKey")] string? subscriptionNotifyApiKey,
        [FromHeader(Name = "Authorization")] string? authorization,
        [FromHeader(Name = "X-Tenant-Id")] string? xTenantId,
        CancellationToken ct)
    {
        string? expectedKey = _config["FhirSubscription:NotifyApiKey"];
        if (!string.IsNullOrEmpty(expectedKey))
            if (subscriptionNotifyApiKey != expectedKey)
                return Unauthorized();

        if (string.IsNullOrEmpty(request.ResourceType) || string.IsNullOrEmpty(request.ResourceUrl))
            return BadRequest("ResourceType and ResourceUrl are required.");

        string? tenantId = request.TenantId ?? xTenantId;

        await _dispatcher.DispatchAsync(
            request.ResourceType,
            request.ResourceUrl,
            tenantId,
            authorization,
            ct);

        return Accepted();
    }
}

public sealed record SubscriptionNotifyRequest(string ResourceType, string ResourceUrl, string? TenantId = null);
