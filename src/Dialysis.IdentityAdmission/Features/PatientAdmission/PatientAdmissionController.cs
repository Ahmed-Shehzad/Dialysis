using Asp.Versioning;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.IdentityAdmission.Features.PatientAdmission;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/patients")]
[Authorize(Policy = "Write")]
public sealed class PatientAdmissionController : ControllerBase
{
    private readonly ISender _sender;

    public PatientAdmissionController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("admit")]
    public async Task<IActionResult> Admit([FromBody] AdmitPatientCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.SendAsync(command, cancellationToken);
        return Ok(result);
    }
}
