using Asp.Versioning;
using Intercessor.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Analytics.Features.Descriptive;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/analytics/descriptive")]
[Authorize(Policy = "Read")]
public sealed class DescriptiveController : ControllerBase
{
    private readonly ISender _sender;

    public DescriptiveController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Session count in date range.</summary>
    [HttpGet("session-count")]
    public async Task<IActionResult> GetSessionCount(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var result = await _sender.SendAsync(new GetSessionCountQuery(from, to), cancellationToken);
        return Ok(new { metric = result.Metric, from = result.From, to = result.To, value = result.Value });
    }

    /// <summary>Hypotension rate: % of sessions with systolic BP &lt; 100 mmHg.</summary>
    [HttpGet("hypotension-rate")]
    public async Task<IActionResult> GetHypotensionRate(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var result = await _sender.SendAsync(new GetHypotensionRateQuery(from, to), cancellationToken);
        return Ok(new
        {
            metric = result.Metric,
            from = result.From,
            to = result.To,
            totalEncounters = result.TotalEncounters,
            encountersWithHypotension = result.EncountersWithHypotension,
            ratePercent = result.RatePercent
        });
    }

    /// <summary>Alert count and median time-to-acknowledgement.</summary>
    [HttpGet("alert-stats")]
    public async Task<IActionResult> GetAlertStats(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var result = await _sender.SendAsync(new GetAlertStatsQuery(from, to), cancellationToken);
        return Ok(new
        {
            metric = result.Metric,
            from = result.From,
            to = result.To,
            totalCount = result.TotalCount,
            activeCount = result.ActiveCount,
            acknowledgedCount = result.AcknowledgedCount,
            medianTimeToAckSeconds = result.MedianTimeToAckSeconds
        });
    }
}
