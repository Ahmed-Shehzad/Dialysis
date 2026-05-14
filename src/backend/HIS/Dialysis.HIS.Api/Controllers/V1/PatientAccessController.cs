using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.PatientAccess.Features.GetPatientPortalSummary;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/patient-access")]
public sealed class PatientAccessController(ICqrsGateway gateway) : HisHateoasControllerBase
{
    [HttpGet("patients/{patientId:guid}/portal-summary")]
    [ProducesResponseType(typeof(ResourceEnvelope<PatientPortalSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPortalSummary(Guid patientId, CancellationToken cancellationToken)
    {
        var dto = await gateway
            .SendQueryAsync<GetPatientPortalSummaryQuery, PatientPortalSummaryDto>(
                new GetPatientPortalSummaryQuery(patientId),
                cancellationToken)
            .ConfigureAwait(false);
        return OkResource(dto);
    }
}
