using Dialysis.HIS.Api.Hateoas;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers;

/// <summary>Hypermedia helpers: every successful JSON payload uses <see cref="ResourceEnvelope{T}"/> with <c>links</c> (HATEOAS).</summary>
public abstract class HisHateoasControllerBase : ControllerBase
{
    protected string ApiVersionSegment =>
        HttpContext.GetRouteValue("version")?.ToString() ?? "1.0";

    protected LinkDto LinkTo(string rel, string pathFromRoot, string method = "GET")
    {
        var path = pathFromRoot.StartsWith('/') ? pathFromRoot : "/" + pathFromRoot;
        return new LinkDto(rel, $"{Request.Scheme}://{Request.Host}{Request.PathBase}{path}", method);
    }

    protected LinkDto LinkCatalog() =>
        LinkTo("ra:catalog", $"/api/v{ApiVersionSegment}/reference-architecture/catalog");

    protected LinkDto LinkCapabilitiesIndex() =>
        LinkTo("ra:capabilities-index", $"/api/v{ApiVersionSegment}/reference-architecture/capabilities", "GET");

    protected LinkDto SelfLink() =>
        new("self", $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}{Request.QueryString}", Request.Method);

    protected IActionResult OkResource<T>(T data, params LinkDto[] additionalLinks)
    {
        var links = new List<LinkDto> { SelfLink() };
        links.Add(LinkCatalog());
        if (additionalLinks.Length > 0)
            links.AddRange(additionalLinks);
        return Ok(new ResourceEnvelope<T>(data, links));
    }

    protected IActionResult CreatedResource<T>(string relativeLocationPath, T data, params LinkDto[] additionalLinks)
    {
        var path = relativeLocationPath.StartsWith('/') ? relativeLocationPath : "/" + relativeLocationPath;
        var location = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{path}";
        var links = new List<LinkDto>
        {
            new("self", location, "GET"),
            LinkCatalog(),
        };
        if (additionalLinks.Length > 0)
            links.AddRange(additionalLinks);
        return Created(location, new ResourceEnvelope<T>(data, links));
    }
}
