using Asp.Versioning;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PublicHealth.Features.Reports;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/reports")]
[Authorize(Policy = "Read")]
public sealed class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Generate and push report to configured PH endpoint. Format: fhir-measure-report.</summary>
    [HttpPost("deliver")]
    [Authorize(Policy = "Write")]
    public async Task<IActionResult> Deliver(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] string format = "fhir-measure-report",
        [FromQuery] string? conditionCode = null,
        [FromBody] GenerateReportRequest? body = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var f = from ?? today.AddDays(-30);
        var t = to ?? today;
        if (f > t) (f, t) = (t, f);

        var result = await _sender.SendAsync(
            new DeliverReportCommand(f, t, format, conditionCode, body?.PatientIds),
            cancellationToken);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { delivered = result.Delivered });
    }

    /// <summary>Match FHIR Condition, Observation, or Procedure against reportable conditions. Body: FHIR resource JSON.</summary>
    [HttpPost("match")]
    [Authorize(Policy = "Read")]
    public async Task<IActionResult> Match(
        [FromQuery] string? jurisdiction = null,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
            return BadRequest(new { error = "Request body must contain FHIR resource (Condition, Observation, or Procedure)" });

        Resource? resource;
        try
        {
            resource = new FhirJsonDeserializer().Deserialize<Resource>(json);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Invalid FHIR resource: {ex.Message}" });
        }

        if (resource is not (Condition or Observation or Procedure))
            return BadRequest(new { error = "Resource must be Condition, Observation, or Procedure" });

        var matches = await _sender.SendAsync(new MatchReportableConditionsQuery(resource, jurisdiction), cancellationToken);
        return Ok(matches.Select(m => new { condition = m.Condition, matchedCode = m.MatchedCode, codeSystem = m.CodeSystem }));
    }

    /// <summary>Generate a public health / registry report. Format: fhir-measure-report.</summary>
    [HttpPost("generate")]
    [Authorize(Policy = "Write")]
    public async Task<IActionResult> Generate(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string format = "fhir-measure-report",
        [FromQuery] string? conditionCode = null,
        [FromBody] GenerateReportRequest? body = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var f = from ?? today.AddDays(-30);
        var t = to ?? today;
        if (f > t) (f, t) = (t, f);

        var result = await _sender.SendAsync(
            new GenerateReportQuery(f, t, format, conditionCode, body?.PatientIds),
            cancellationToken);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        if (result.Content == null)
            return StatusCode(500, new { error = "Report generation failed" });

        var contentType = format.Contains("json", StringComparison.OrdinalIgnoreCase) ? "application/json" : "application/octet-stream";
        Response.ContentType = contentType;
        if (!string.IsNullOrEmpty(result.Filename))
            Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{result.Filename}\"");

        await result.Content.CopyToAsync(Response.Body, cancellationToken);
        return new EmptyResult();
    }
}

public sealed record GenerateReportRequest(IReadOnlyList<string>? PatientIds);
