using Microsoft.AspNetCore.Mvc;

namespace Dialysis.IdentityAdmission.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok();
    }
}
