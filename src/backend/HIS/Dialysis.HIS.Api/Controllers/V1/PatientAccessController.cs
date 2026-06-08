using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.PatientAccess.Features.GetPatientPortalSummary;
using Dialysis.HIS.PatientAccess.Features.ListPortalPatients;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/patient-access")]
public sealed class PatientAccessController : HisHateoasControllerBase
{
    private readonly ICqrsGateway _gateway;
    public PatientAccessController(ICqrsGateway gateway) => _gateway = gateway;

    /// <summary>
    /// Lists patient ids that have portal-relevant data, so the single-patient portal can discover a
    /// patient to open when the caller has no patient claim (staff/dev sessions, smoke checks).
    /// </summary>
    [HttpGet("patients")]
    [ProducesResponseType(typeof(ResourceEnvelope<IReadOnlyList<Guid>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPortalPatientsAsync([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var ids = await _gateway
            .SendQueryAsync<ListPortalPatientsQuery, IReadOnlyList<Guid>>(
                new ListPortalPatientsQuery(take), cancellationToken)
            .ConfigureAwait(false);
        return OkResource(ids);
    }

    [HttpGet("patients/{patientId:guid}/portal-summary")]
    [ProducesResponseType(typeof(ResourceEnvelope<PatientPortalSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPortalSummaryAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var dto = await _gateway
            .SendQueryAsync<GetPatientPortalSummaryQuery, PatientPortalSummaryDto>(
                new GetPatientPortalSummaryQuery(patientId),
                cancellationToken)
            .ConfigureAwait(false);
        return OkResource(dto);
    }
}
