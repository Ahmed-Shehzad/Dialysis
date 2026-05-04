using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Controllers;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.PatientFlow.Features.AdmitPatient;
using Dialysis.HIS.PatientFlow.Features.CreateReferral;
using Dialysis.HIS.PatientFlow.Features.DischargePatient;
using Dialysis.HIS.PatientFlow.Features.RegisterPatient;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

/// <summary>RA: <em>Patient monitoring</em> — ADT, referrals, care pathway (Tummers et al., 2021).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/patient-flow/patients")]
public sealed class PatientFlowPatientsController(ICqrsGateway gateway) : HisHateoasControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ResourceEnvelope<RegisterPatientResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterPatient([FromBody] RegisterPatientCommand command, CancellationToken cancellationToken)
    {
        var id = await gateway.SendCommandAsync<RegisterPatientCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        var location = $"{Request.Path}/{id}";
        return CreatedResource(location, new RegisterPatientResponse(id), LinkCapabilitiesIndex());
    }

    [HttpPost("{patientId:guid}/admit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Admit(Guid patientId, CancellationToken cancellationToken)
    {
        await gateway.SendCommandAsync<AdmitPatientCommand, Unit>(new AdmitPatientCommand(patientId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{patientId:guid}/discharge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Discharge(Guid patientId, CancellationToken cancellationToken)
    {
        await gateway.SendCommandAsync<DischargePatientCommand, Unit>(new DischargePatientCommand(patientId), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("{patientId:guid}/referrals")]
    [ProducesResponseType(typeof(ResourceEnvelope<CreateReferralResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateReferral(
        Guid patientId,
        [FromBody] CreateReferralRequest body,
        CancellationToken cancellationToken)
    {
        var id = await gateway
            .SendCommandAsync<CreateReferralCommand, Guid>(new CreateReferralCommand(patientId, body.ReferralTypeCode), cancellationToken)
            .ConfigureAwait(false);
        var location = $"{Request.Path}/{id}";
        return CreatedResource(location, new CreateReferralResponse(id), LinkCapabilitiesIndex());
    }

    public sealed record RegisterPatientResponse(Guid Id);

    public sealed record CreateReferralRequest(string ReferralTypeCode);

    public sealed record CreateReferralResponse(Guid Id);
}
