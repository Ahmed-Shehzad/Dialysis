using Asp.Versioning;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Registry.Features.BatchExport;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/registry")]
[Authorize(Policy = "Read")]
public sealed class BatchExportController : ControllerBase
{
    private readonly ISender _sender;

    public BatchExportController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Batch export for registry submission. Adapters: ESRD, QIP, CROWNWeb, NHSN, VascularAccess. Use format=hl7v2 for ESRD HL7 v2 output.</summary>
    [HttpGet("export")]
    [Authorize(Policy = "Write")]
    public async Task<IActionResult> Export(
        [FromQuery] string adapter = "ESRD",
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] string? format = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var f = from ?? today.AddDays(-30);
        var t = to ?? today;
        if (f > t) (f, t) = (t, f);

        var result = await _sender.SendAsync(new BatchExportQuery(adapter, f, t, null, format), cancellationToken);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        if (result.Content == null)
            return StatusCode(500, new { error = "Export failed" });

        var contentType = result.Filename switch
        {
            not null when result.Filename.EndsWith(".hl7", StringComparison.OrdinalIgnoreCase) => "text/plain",
            not null when result.Filename.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) => "text/csv",
            _ => "application/ndjson"
        };
        Response.ContentType = contentType;
        if (!string.IsNullOrEmpty(result.Filename))
            Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{result.Filename}\"");

        await result.Content.CopyToAsync(Response.Body, cancellationToken);
        return new EmptyResult();
    }

    /// <summary>List available registry adapters.</summary>
    [HttpGet("adapters")]
    public IActionResult ListAdapters()
    {
        return Ok(new[]
        {
            new { name = "ESRD", description = "End-Stage Renal Disease (NDJSON or HL7v2 via format=hl7v2)" },
            new { name = "QIP", description = "Quality Incentive Program (CSV)" },
            new { name = "CROWNWeb", description = "CMS CROWNWeb (CSV for CMS-2728)" },
            new { name = "NHSN", description = "CDC NHSN Dialysis Event (CSV - infection events, vascular access)" },
            new { name = "VascularAccess", description = "Vascular access procedures (CSV - fistula, graft, catheter)" }
        });
    }
}
