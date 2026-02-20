using Dialysis.Fhir.Api.Subscriptions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Fhir.Api.Controllers;

/// <summary>
/// Internal endpoint for services to notify the subscription dispatcher when FHIR resources are created/updated.
/// Protected by FhirSubscription:NotifyApiKey (X-Subscription-Notify-ApiKey header). Key is required in non-Development environments.
/// </summary>
[ApiController]
[Route("api/fhir/subscription-notify")]
public sealed class SubscriptionNotifyController : ControllerBase
{
    private readonly SubscriptionDispatcher _dispatcher;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public SubscriptionNotifyController(
        SubscriptionDispatcher dispatcher,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        _dispatcher = dispatcher;
        _config = config;
        _env = env;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> NotifyAsync(
        [FromBody] SubscriptionNotifyRequest request,
        [FromHeader(Name = "X-Subscription-Notify-ApiKey")] string? subscriptionNotifyApiKey,
        [FromHeader(Name = "Authorization")] string? authorization,
        [FromHeader(Name = "X-Tenant-Id")] string? xTenantId,
        CancellationToken ct)
    {
        string? expectedKey = _config["FhirSubscription:NotifyApiKey"];
        if (!_env.IsDevelopment() && string.IsNullOrEmpty(expectedKey))
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                "FhirSubscription:NotifyApiKey must be configured in production.");

        if (!string.IsNullOrEmpty(expectedKey) && subscriptionNotifyApiKey != expectedKey)
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
