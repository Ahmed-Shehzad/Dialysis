using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.PatientFlow.Features.AdmitPatient;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/patient-flow")]
public sealed class PatientFlowController(ICqrsGateway gateway) : HisHateoasControllerBase
{
    [HttpPost("admissions")]
    [ProducesResponseType(typeof(ResourceEnvelope<AdmitPatientResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AdmitPatientAsync(
        [FromBody] AdmitPatientCommand command,
        CancellationToken cancellationToken)
    {
        var id = await gateway.SendCommandAsync<AdmitPatientCommand, Guid>(command, cancellationToken).ConfigureAwait(false);
        return CreatedResource(
            $"/api/v{ApiVersionSegment}/patient-flow/admissions/{id}",
            new AdmitPatientResponse(id));
    }

    public sealed record AdmitPatientResponse(Guid Id);
}
