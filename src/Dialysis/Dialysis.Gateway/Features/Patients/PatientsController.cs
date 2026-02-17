using Asp.Versioning;
using Dialysis.DeviceIngestion.Features.Patients.Create;
using Dialysis.DeviceIngestion.Features.Patients.Delete;
using Dialysis.DeviceIngestion.Features.Patients.Get;
using Dialysis.DeviceIngestion.Features.Patients.List;
using Dialysis.DeviceIngestion.Features.Patients.Update;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Patients;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/patients")]
public sealed class PatientsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ITenantContext _tenantContext;

    public PatientsController(ISender sender, ITenantContext tenantContext)
    {
        _sender = sender;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Create a patient. Returns 409 if logicalId already exists. Include X-Tenant-Id header.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PatientResponse>> Create(
        [FromBody] CreatePatientRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.LogicalId))
            return BadRequest(new { error = "LogicalId is required." });

        var tenantId = _tenantContext.TenantId;
        var logicalId = new PatientId(request.LogicalId);

        var command = new CreatePatientCommand(
            tenantId,
            logicalId,
            request.FamilyName,
            request.GivenNames,
            request.BirthDate);

        var result = await _sender.SendAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { logicalId = result.LogicalId.Value }, new PatientResponse(
            result.LogicalId.Value,
            request.FamilyName,
            request.GivenNames,
            request.BirthDate));
    }

    /// <summary>
    /// Get a patient by logical ID. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet("{logicalId}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientResponse>> Get(
        string logicalId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(logicalId);

        var query = new GetPatientQuery(tenantId, patientId);
        var patient = await _sender.SendAsync(query, cancellationToken);

        if (patient is null)
            return NotFound();

        return Ok(new PatientResponse(
            patient.LogicalId.Value,
            patient.FamilyName,
            patient.GivenNames,
            patient.BirthDate));
    }

    /// <summary>
    /// Update a patient. Include X-Tenant-Id header.
    /// </summary>
    [HttpPut("{logicalId}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientResponse>> Update(
        string logicalId,
        [FromBody] UpdatePatientRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(logicalId);

        var command = new UpdatePatientCommand(tenantId, patientId, request.FamilyName, request.GivenNames, request.BirthDate);
        var result = await _sender.SendAsync(command, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(new PatientResponse(
            result.LogicalId.Value,
            result.FamilyName,
            result.GivenNames,
            result.BirthDate));
    }

    /// <summary>
    /// Delete a patient. Include X-Tenant-Id header.
    /// </summary>
    [HttpDelete("{logicalId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string logicalId, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(logicalId);

        var command = new DeletePatientCommand(tenantId, patientId);
        var deleted = await _sender.SendAsync(command, cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// List patients with optional search. Query params: family, given, _count, _offset. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ListPatientsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListPatientsResponse>> List(
        [FromQuery] string? family,
        [FromQuery] string? given,
        [FromQuery] int? _count,
        [FromQuery] int _offset = 0,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var query = new ListPatientsQuery(tenantId, family, given, _count, _offset);
        var patients = await _sender.SendAsync(query, cancellationToken);

        var items = patients.Select(p => new PatientResponse(
            p.LogicalId.Value,
            p.FamilyName,
            p.GivenNames,
            p.BirthDate)).ToList();

        return Ok(new ListPatientsResponse(items));
    }
}
