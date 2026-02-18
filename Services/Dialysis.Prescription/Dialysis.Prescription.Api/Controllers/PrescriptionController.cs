using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Dialysis.Hl7ToFhir;
using Dialysis.Prescription.Application.Features.GetPrescriptionByMrn;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Prescription.Api.Controllers;

[ApiController]
[Route("api/prescriptions")]
[Authorize(Policy = "PrescriptionRead")]
public sealed class PrescriptionController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IAuditRecorder _audit;
    private readonly ITenantContext _tenant;

    public PrescriptionController(ISender sender, IAuditRecorder audit, ITenantContext tenant)
    {
        _sender = sender;
        _audit = audit;
        _tenant = tenant;
    }

    [HttpGet("{mrn}")]
    [ProducesResponseType(typeof(GetPrescriptionByMrnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByMrnAsync(string mrn, CancellationToken cancellationToken)
    {
        var query = new GetPrescriptionByMrnQuery(mrn);
        GetPrescriptionByMrnResponse? response = await _sender.SendAsync(query, cancellationToken);
        if (response is not null)
            await _audit.RecordAsync(new AuditRecordRequest(
                AuditAction.Read, "Prescription", mrn, User.Identity?.Name,
                AuditOutcome.Success, "Prescription retrieval by MRN", _tenant.TenantId), cancellationToken);

        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("{mrn}/fhir")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByMrnFhirAsync(string mrn, CancellationToken cancellationToken)
    {
        var query = new GetPrescriptionByMrnQuery(mrn);
        GetPrescriptionByMrnResponse? response = await _sender.SendAsync(query, cancellationToken);
        if (response is null)
            return NotFound();

        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Read, "Prescription", mrn, User.Identity?.Name,
            AuditOutcome.Success, "Prescription FHIR retrieval by MRN", _tenant.TenantId), cancellationToken);

        var input = new PrescriptionMappingInput(
            response.OrderId,
            mrn,
            response.TherapyModality,
            null,
            response.BloodFlowRateMlMin,
            response.UfRateMlH,
            response.UfTargetVolumeMl,
            null);
        Hl7.Fhir.Model.ServiceRequest fhirServiceRequest = PrescriptionMapper.ToFhirServiceRequest(input);
        fhirServiceRequest.Id = response.OrderId;
        string json = FhirJsonHelper.ToJson(fhirServiceRequest);
        return Content(json, "application/fhir+json");
    }
}
