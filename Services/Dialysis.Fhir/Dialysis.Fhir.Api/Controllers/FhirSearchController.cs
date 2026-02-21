using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Hl7.Fhir.Model;

namespace Dialysis.Fhir.Api.Controllers;

/// <summary>
/// FHIR-type unified search. GET /api/fhir/{resourceType} with resource-specific query params.
/// Supports: Patient, Device, ServiceRequest, Procedure, Observation, DetectedIssue, AuditEvent.
/// </summary>
[ApiController]
[Route("api/fhir")]
[Authorize(Policy = "FhirExport")]
public sealed class FhirSearchController : ControllerBase
{
    private readonly FhirBulkExportService _exportService;

    public FhirSearchController(FhirBulkExportService exportService)
    {
        _exportService = exportService;
    }

    /// <summary>
    /// FHIR search for a resource type. Returns a search set Bundle.
    /// Params: Patient (_id, identifier, _count); Device (_id, identifier);
    /// ServiceRequest (subject, patient, _count); Procedure/Observation (subject, patient, date, dateFrom, dateTo, _count);
    /// DetectedIssue (_id, device, date, from, to, _count); AuditEvent (_count).
    /// </summary>
    [HttpGet("{resourceType}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK, "application/fhir+json")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchAsync(
        [FromRoute] string resourceType,
        CancellationToken cancellationToken = default)
    {
        var @params = Request.Query.Keys.ToDictionary(k => k!, k => (string?)Request.Query[k].ToString());
        try
        {
            Bundle bundle = await _exportService.SearchAsync(resourceType, @params, Request, cancellationToken);
            string json = Hl7ToFhir.FhirJsonHelper.ToJson(bundle);
            return Content(json, "application/fhir+json");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
