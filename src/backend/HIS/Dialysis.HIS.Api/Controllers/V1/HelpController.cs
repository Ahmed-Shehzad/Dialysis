using Asp.Versioning;
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

    public sealed record HelpDiscoveryDto
    {
        public HelpDiscoveryDto(string Title, IReadOnlyList<DocumentationRefDto> Documentation)
        {
            this.Title = Title;
            this.Documentation = Documentation;
        }
        public string Title { get; init; }
        public IReadOnlyList<DocumentationRefDto> Documentation { get; init; }
        public void Deconstruct(out string Title, out IReadOnlyList<DocumentationRefDto> Documentation)
        {
            Title = this.Title;
            Documentation = this.Documentation;
        }
    }

    public sealed record DocumentationRefDto
    {
        public DocumentationRefDto(string Title, string RepositoryRelativePath)
        {
            this.Title = Title;
            this.RepositoryRelativePath = RepositoryRelativePath;
        }
        public string Title { get; init; }
        public string RepositoryRelativePath { get; init; }
        public void Deconstruct(out string Title, out string RepositoryRelativePath)
        {
            Title = this.Title;
            RepositoryRelativePath = this.RepositoryRelativePath;
        }
    }
}
