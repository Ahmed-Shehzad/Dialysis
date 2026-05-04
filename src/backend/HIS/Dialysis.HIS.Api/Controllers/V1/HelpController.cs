using Asp.Versioning;
using Dialysis.HIS.Api.Controllers;
using Dialysis.HIS.Api.Hateoas;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

/// <summary>
/// RA <em>Data management</em> — <strong>Help</strong> (Fig. 6). Discovery links for OpenAPI, RA views, and in-repo documentation paths.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/help")]
public sealed class HelpController : HisHateoasControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ResourceEnvelope<HelpDiscoveryDto>), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var v = ApiVersionSegment;
        var data = new HelpDiscoveryDto(
            Title: "HIS API discovery",
            Documentation:
            [
                new DocumentationRefDto("RA sub-module traceability (34)", "src/backend/HIS/his_ra_submodules.md"),
                new DocumentationRefDto("Production security backlog", "src/backend/HIS/his_production_security_backlog.md"),
                new DocumentationRefDto("Integration backlog", "src/backend/HIS/his_integration_backlog.md"),
            ]);
        return OkResource(
            data,
            LinkTo("openapi", $"/openapi/v{v}.json"),
            LinkTo("health", "/health"),
            LinkCapabilitiesIndex());
    }

    public sealed record HelpDiscoveryDto(string Title, IReadOnlyList<DocumentationRefDto> Documentation);

    public sealed record DocumentationRefDto(string Title, string RepositoryRelativePath);
}
