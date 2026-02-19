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
    /// _patient: Patient compartment filter (Patient/id or id) - restricts to resources for that patient.
    /// _since: ISO 8601 - resources modified/created after this time (passed to Treatment, Alarm backends).
    /// </summary>
    [HttpGet("$export")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK, "application/fhir+json")]
    public async Task<IActionResult> ExportAsync(
        [FromQuery(Name = "_type")] string? type = "Patient,Device,ServiceRequest,Procedure,Observation,DetectedIssue",
        [FromQuery(Name = "_limit")] int limitParam = 1000,
        [FromQuery(Name = "_patient")] string? patient = null,
        [FromQuery(Name = "_since")] DateTimeOffset? since = null,
        CancellationToken cancellationToken = default)
    {
        string[] types = (type ?? "Patient").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int limit = Math.Min(Math.Max(1, limitParam), 10_000);

        string? patientId = ExtractPatientId(patient);

        Bundle bundle = await _exportService.ExportAsync(types, limit, patientId, since, Request, cancellationToken);

        string json = Dialysis.Hl7ToFhir.FhirJsonHelper.ToJson(bundle);
        return Content(json, "application/fhir+json");
    }

    private static string? ExtractPatientId(string? patient)
    {
        if (string.IsNullOrWhiteSpace(patient)) return null;
        if (patient.StartsWith("Patient/", StringComparison.OrdinalIgnoreCase))
            return patient["Patient/".Length..].Trim();
        return patient.Trim();
    }
}
