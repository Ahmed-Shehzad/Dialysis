using Asp.Versioning;
using Dialysis.HIS.Api.Hateoas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Dialysis.HIS.Api.Controllers;

/// <summary>Host probe; not versioned so load balancers stay stable.</summary>
[ApiController]
[ApiVersionNeutral]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly IOptions<ApiVersioningOptions> _apiVersioningOptions;
    /// <summary>Host probe; not versioned so load balancers stay stable.</summary>
    public HealthController(IOptions<ApiVersioningOptions> apiVersioningOptions) => _apiVersioningOptions = apiVersioningOptions;

    private string DefaultApiVersionUrlSegment =>
        _apiVersioningOptions.Value.DefaultApiVersion.ToString();

    [HttpGet]
    [ProducesResponseType(typeof(ResourceEnvelope<HealthStatusDto>), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var data = new HealthStatusDto("healthy", "HIS");
        var v = DefaultApiVersionUrlSegment;
        var links = new List<LinkDto>
        {
            new("self", $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}", "GET"),
            new("ra:catalog", $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/v{v}/reference-architecture/catalog", "GET"),
            new("ra:capabilities-index", $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/v{v}/reference-architecture/capabilities", "GET"),
        };
        return Ok(new ResourceEnvelope<HealthStatusDto>(data, links));
    }

    public sealed record HealthStatusDto
    {
        public HealthStatusDto(string Status, string Module)
        {
            this.Status = Status;
            this.Module = Module;
        }
        public string Status { get; init; }
        public string Module { get; init; }
        public void Deconstruct(out string status, out string module)
        {
            status = Status;
            module = Module;
        }
    }
}
