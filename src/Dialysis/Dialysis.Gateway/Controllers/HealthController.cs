using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        _logger.LogDebug("Health check");
        return Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow });
    }
}
