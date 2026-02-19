using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Hl7.Fhir.Model;

namespace Dialysis.Fhir.Api.Controllers;

[ApiController]
[Route("api/fhir")]
[Authorize(Policy = "FhirExport")]
public sealed class FhirExportController : ControllerBase
{
    private readonly FhirBulkExportService _exportService;

    public FhirExportController(FhirBulkExportService exportService)
    {
        _exportService = exportService;
    }

    /// <summary>
    /// FHIR bulk export - returns a Bundle containing resources of the requested types.
    /// Supported _type: Patient, Device, ServiceRequest, Procedure, Observation, DetectedIssue, AuditEvent.
    /// </summary>
    [HttpGet("$export")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK, "application/fhir+json")]
    public async Task<IActionResult> ExportAsync(
        [FromQuery(Name = "_type")] string? type = "Patient,Device,ServiceRequest,Procedure,Observation,DetectedIssue",
        [FromQuery(Name = "_limit")] int limitParam = 1000,
        CancellationToken cancellationToken = default)
    {
        string[] types = (type ?? "Patient").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int limit = Math.Min(Math.Max(1, limitParam), 10_000);

        Bundle bundle = await _exportService.ExportAsync(types, limit, Request, cancellationToken);

        string json = Dialysis.Hl7ToFhir.FhirJsonHelper.ToJson(bundle);
        return Content(json, "application/fhir+json");
    }
}
