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

    public PrescriptionController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{mrn}")]
    [ProducesResponseType(typeof(GetPrescriptionByMrnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByMrnAsync(string mrn, CancellationToken cancellationToken)
    {
        var query = new GetPrescriptionByMrnQuery(mrn);
        GetPrescriptionByMrnResponse? response = await _sender.SendAsync(query, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
