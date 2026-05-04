using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.HIS.Api.Authorization;
using Dialysis.HIS.Api.Controllers;
using Dialysis.HIS.Api.Hateoas;
using Dialysis.HIS.PatientAccess.Features.GetPatientPortalSummary;
using Dialysis.HIS.PatientAccess.Features.RequestAppointment;
using Dialysis.HIS.PatientAccess.Ports;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.HIS.Api.Controllers.V1;

/// <summary>RA: <em>Patient monitoring</em> — patient-facing channel (portal; Tummers et al., 2021).</summary>
[ApiController]
[ApiVersion("1.0")]
[ServiceFilter(typeof(PatientPortalPatientScopeFilter))]
[Route("api/v{version:apiVersion}/patient-portal/patients")]
public sealed class PatientPortalController(ICqrsGateway gateway) : HisHateoasControllerBase
{
    [HttpGet("{patientId:guid}/summary")]
    [ProducesResponseType(typeof(ResourceEnvelope<PatientPortalSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetSummary(Guid patientId, CancellationToken cancellationToken)
    {
        var summary = await gateway
            .SendQueryAsync<GetPatientPortalSummaryQuery, PatientPortalSummaryDto?>(new GetPatientPortalSummaryQuery(patientId), cancellationToken)
            .ConfigureAwait(false);
        return summary is null ? NoContent() : OkResource(summary, LinkCapabilitiesIndex());
    }

    [HttpPost("{patientId:guid}/appointment-requests")]
    [ProducesResponseType(typeof(ResourceEnvelope<RequestAppointmentResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RequestAppointment(
        Guid patientId,
        [FromBody] RequestAppointmentBody body,
        CancellationToken cancellationToken)
    {
        var id = await gateway
            .SendCommandAsync<RequestPatientAppointmentCommand, Guid>(new RequestPatientAppointmentCommand(patientId, body.Notes), cancellationToken)
            .ConfigureAwait(false);
        return CreatedResource($"{Request.Path}/{id}", new RequestAppointmentResponse(id), LinkCapabilitiesIndex());
    }

    public sealed record RequestAppointmentBody(string Notes);

    public sealed record RequestAppointmentResponse(Guid Id);
}
