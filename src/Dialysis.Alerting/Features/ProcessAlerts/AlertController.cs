using Asp.Versioning;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Alerting.Features.ProcessAlerts;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/alerts")]
[Authorize(Policy = "Read")]
public sealed class AlertController : ControllerBase
{
    private readonly ISender _sender;

    public AlertController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AlertStatusFilter? status, CancellationToken cancellationToken)
    {
        var result = await _sender.SendAsync(new ListAlertsQuery { Status = status }, cancellationToken);
        return Ok(result.Alerts);
    }

    [HttpPost]
    [Authorize(Policy = "Write")]
    public async Task<IActionResult> Create([FromBody] CreateAlertCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.SendAsync(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{alertId}/acknowledge")]
    [Authorize(Policy = "Write")]
    public async Task<IActionResult> Acknowledge(string alertId, CancellationToken cancellationToken)
    {
        await _sender.SendAsync(new AcknowledgeAlertCommand { AlertId = alertId }, cancellationToken);
        return Ok();
    }
}
