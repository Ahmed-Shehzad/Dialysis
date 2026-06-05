using Asp.Versioning;
using Dialysis.PDMS.Composition.Demo;
using Dialysis.PDMS.Persistence;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.PDMS.Api.Controllers.V1;

/// <summary>
/// Development-only demo controls. Every action is gated behind <c>Pdms:Demo:Enabled</c> and
/// returns 404 when the demo is off, so the surface simply does not exist in production.
/// Backs the SPA's <c>/demo</c> control panel.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/demo")]
public sealed class DemoController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly PdmsDbContext _db;
    private readonly IDialysisSessionRepository _sessions;
    private readonly TimeProvider _time;

    /// <summary>Creates the controller.</summary>
    public DemoController(
        IConfiguration configuration,
        PdmsDbContext db,
        IDialysisSessionRepository sessions,
        TimeProvider time)
    {
        _configuration = configuration;
        _db = db;
        _sessions = sessions;
        _time = time;
    }

    /// <summary>
    /// Wipes every dialysis session + reading and repaints the demo snapshot (1 in-progress,
    /// 1 paused, 2 scheduled-for-stage). Lets a presenter return to a clean starting state
    /// between runs; the autopilot then resumes producing completed sessions + invoices.
    /// </summary>
    [HttpPost("reset-sessions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetSessionsAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue("Pdms:Demo:Enabled", false))
            return NotFound();

        // Readings reference sessions, so clear them first.
        await _db.Readings.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await _db.Sessions.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        await PdmsDemoSeeder.SeedSnapshotAsync(_sessions, _db, _time, cancellationToken).ConfigureAwait(false);

        var sessions = await _db.Sessions.CountAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new { reseeded = true, sessions });
    }
}
