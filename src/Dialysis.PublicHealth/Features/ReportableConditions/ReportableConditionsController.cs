using Asp.Versioning;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PublicHealth.Features.ReportableConditions;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/reportable-conditions")]
[Authorize(Policy = "Read")]
public sealed class ReportableConditionsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportableConditionsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>List reportable conditions (e.g. hepatitis, HIV, ESRD). Filter by ?jurisdiction=US|DE|UK.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? jurisdiction = null, CancellationToken cancellationToken = default)
    {
        var conditions = await _sender.SendAsync(new ListReportableConditionsQuery(jurisdiction), cancellationToken);
        return Ok(conditions);
    }
}
