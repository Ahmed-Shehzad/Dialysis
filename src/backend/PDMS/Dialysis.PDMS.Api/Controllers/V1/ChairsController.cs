using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.PDMS.TreatmentSessions.Features.ListChairAssignments;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PDMS.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/chairs")]
public sealed class ChairsController : ControllerBase
{
    private readonly ICqrsGateway _gateway;
    public ChairsController(ICqrsGateway gateway) => _gateway = gateway;
    /// <summary>
    /// Returns the current chair occupancy snapshot — one entry per chair that's been placed
    /// since the API started. Drives the chairside dashboard and any future floor map. The
    /// projection is in-memory; restarts clear it, and the next placement event from HIS will
    /// re-hydrate the chairs in use.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ChairAssignmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var assignments = await _gateway
            .SendQueryAsync<ListChairAssignmentsQuery, IReadOnlyList<ChairAssignmentDto>>(
                new ListChairAssignmentsQuery(), cancellationToken)
            .ConfigureAwait(false);
        return Ok(assignments);
    }
}
