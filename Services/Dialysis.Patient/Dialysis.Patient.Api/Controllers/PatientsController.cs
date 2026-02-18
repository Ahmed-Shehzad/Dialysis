using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Dialysis.Patient.Api.Contracts;
using Dialysis.Patient.Application.Domain.ValueObjects;
using Dialysis.Patient.Application.Features.GetPatientByMrn;
using Dialysis.Patient.Application.Features.RegisterPatient;
using Dialysis.Patient.Application.Features.SearchPatients;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Patient.Api.Controllers;

[ApiController]
[Route("api/patients")]
public sealed class PatientsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IAuditRecorder _audit;
    private readonly ITenantContext _tenant;

    public PatientsController(ISender sender, IAuditRecorder audit, ITenantContext tenant)
    {
        _sender = sender;
        _audit = audit;
        _tenant = tenant;
    }

    [HttpGet("mrn/{mrn}")]
    [Authorize(Policy = "PatientRead")]
    [ProducesResponseType(typeof(GetPatientByMrnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByMrnAsync(string mrn, CancellationToken cancellationToken)
    {
        var query = new GetPatientByMrnQuery(new MedicalRecordNumber(mrn));
        GetPatientByMrnResponse? response = await _sender.SendAsync(query, cancellationToken);
        if (response is not null)
            await _audit.RecordAsync(new AuditRecordRequest(
                AuditAction.Read, "Patient", mrn, User.Identity?.Name,
                AuditOutcome.Success, "Patient retrieval by MRN", _tenant.TenantId), cancellationToken);

        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("search")]
    [Authorize(Policy = "PatientRead")]
    [ProducesResponseType(typeof(SearchPatientsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] string firstName,
        [FromQuery] string lastName,
        CancellationToken cancellationToken)
    {
        var query = new SearchPatientsQuery(new Person(firstName, lastName));
        SearchPatientsResponse response = await _sender.SendAsync(query, cancellationToken);
        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Read, "Patient", null, User.Identity?.Name,
            AuditOutcome.Success, "Patient search", _tenant.TenantId), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = "PatientWrite")]
    [ProducesResponseType(typeof(RegisterPatientResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterPatientRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterPatientCommand(
            new MedicalRecordNumber(request.MedicalRecordNumber),
            new Person(request.FirstName, request.LastName),
            request.DateOfBirth,
            request.Gender is not null ? new Gender(request.Gender) : null);

        RegisterPatientResponse response = await _sender.SendAsync(command, cancellationToken);
        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Create, "Patient", response.Id, User.Identity?.Name,
            AuditOutcome.Success, "Patient registration", _tenant.TenantId), cancellationToken);
        return CreatedAtAction(nameof(GetByMrnAsync), new { mrn = request.MedicalRecordNumber }, response);
    }
}
