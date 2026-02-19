using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Dialysis.Hl7ToFhir;
using Dialysis.Prescription.Application.Features.GetPrescriptionByMrn;
using Dialysis.Prescription.Application.Features.GetPrescriptions;

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

    [HttpGet("fhir")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPrescriptionsFhirAsync(
        [FromQuery] int limit = 1000,
        [FromQuery] string? subject = null,
        [FromQuery] string? patient = null,
        CancellationToken cancellationToken = default)
    {
        var mrn = !string.IsNullOrWhiteSpace(subject) ? subject : patient;
        MedicalRecordNumber? mrnVal = !string.IsNullOrWhiteSpace(mrn) ? new MedicalRecordNumber(mrn) : null;
        var query = new GetPrescriptionsQuery(Math.Min(limit, 10_000), mrnVal);
        GetPrescriptionsResponse response = await _sender.SendAsync(query, cancellationToken);
        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Read, "Prescription", null, User.Identity?.Name,
            AuditOutcome.Success, $"FHIR prescriptions ({response.Prescriptions.Count})", _tenant.TenantId), cancellationToken);

        var bundle = new Hl7.Fhir.Model.Bundle
        {
            Type = Hl7.Fhir.Model.Bundle.BundleType.Collection,
            Entry = response.Prescriptions.Select(p =>
            {
                var input = new PrescriptionMappingInput(
                    p.OrderId,
                    p.PatientMrn,
                    p.Modality,
                    p.OrderingProvider,
                    p.BloodFlowRateMlMin,
                    p.UfRateMlH,
                    p.UfTargetVolumeMl,
                    p.ReceivedAt);
                var fhir = PrescriptionMapper.ToFhirServiceRequest(input);
                fhir.Id = p.OrderId;
                return new Hl7.Fhir.Model.Bundle.EntryComponent
                {
                    FullUrl = $"urn:uuid:prescription-{p.OrderId}",
                    Resource = fhir
                };
            }).ToList()
        };
        string json = FhirJsonHelper.ToJson(bundle);
        return Content(json, "application/fhir+json");
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
