using Dialysis.Gateway.Infrastructure;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Dialysis.Gateway.Features.Smart;

/// <summary>
/// SMART on FHIR discovery. Enables EHRs to find authorization endpoints. C5: AllowAnonymous (discovery).
/// GET /.well-known/smart-configuration
/// </summary>
[ApiController]
[Route(".well-known")]
[AllowAnonymous]
public sealed class SmartConfigurationController : ControllerBase
{
    private readonly ISender _sender;
    private readonly SmartServerOptions _options;

    public SmartConfigurationController(ISender sender, IOptions<SmartServerOptions> options)
    {
        _sender = sender;
        _options = options.Value;
    }

    [HttpGet("smart-configuration")]
    [Produces("application/json")]
    public async Task<IActionResult> GetSmartConfiguration(CancellationToken ct)
    {
        var baseUrl = _options.BaseUrl ?? $"{Request.Scheme}://{Request.Host}";
        var config = await _sender.SendAsync(new GetSmartConfigurationQuery(baseUrl), ct);
        return Ok(config);
    }
}
