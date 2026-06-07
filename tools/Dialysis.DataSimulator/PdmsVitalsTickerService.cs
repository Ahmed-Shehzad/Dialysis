using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.DataSimulator;

/// <summary>
/// Restores the live intradialytic telemetry that the deleted #167 demo services
/// (<c>VitalsTickerService</c> / <c>MachineTelemetrySimulatorService</c>) used to produce: on a
/// short cadence it records a fresh reading for every in-progress PDMS session, which makes the
/// <c>RecordReading</c> handler broadcast it over the chairside vitals SignalR hub. Any session a
/// user has in progress (or one the journey worker started) gets a live waveform.
/// </summary>
public sealed class PdmsVitalsTickerService : BackgroundService
{
    // 2-second cadence: the chairside vitals waveform should update in near-real-time. The
    // RecordReading handler broadcasts each reading synchronously over the SignalR vitals hub,
    // so the client sees a fresh point roughly every TickInterval.
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(2);

    private readonly IServiceProvider _services;
    private readonly DataSimulatorOptions _options;
    private readonly ILogger<PdmsVitalsTickerService> _logger;

    /// <summary>Creates the ticker.</summary>
    public PdmsVitalsTickerService(
        IServiceProvider services,
        IOptions<DataSimulatorOptions> options,
        ILogger<PdmsVitalsTickerService> logger)
    {
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("PDMS vitals ticker is disabled (DataSimulator:Enabled=false); idling.");
            return;
        }

        _logger.LogInformation("PDMS vitals ticker started: recording a reading for each in-progress session every {Interval}s.", TickInterval.TotalSeconds);
        using var timer = new PeriodicTimer(TickInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await TickAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "PDMS vitals tick failed; will retry on the next interval.");
            }
        }
    }

    // Upper bound on how many sessions one tick fans out to. The journey keeps the live pool small,
    // but if a backlog ever builds this caps the per-tick work to the most-recent chairs so a single
    // round always completes well inside the 2s cadence (writes run concurrently, not serially —
    // serial writes against a large pool were what previously starved the newest session).
    private const int MaxSessionsPerTick = 24;

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var pdms = scope.ServiceProvider.GetRequiredService<IPdmsClient>();

        var sessions = await pdms.ListInProgressSessionsAsync(cancellationToken).ConfigureAwait(false);
        if (sessions.Count == 0)
            return;

        // ListInProgressSessions is oldest-first; the tail is the most-recently-started chairs — the
        // ones a clinician is actually watching — so cap to those when a backlog exists.
        var targets = sessions.Count <= MaxSessionsPerTick
            ? sessions
            : sessions.Skip(sessions.Count - MaxSessionsPerTick).ToList();

        // Fan out concurrently so the whole round finishes in roughly one request's time, keeping the
        // per-session cadence at ~TickInterval regardless of how many chairs are live.
        var results = await Task.WhenAll(targets.Select(async session =>
        {
            try
            {
                await pdms.RecordReadingAsync(session.Id, NextReading(session.Id), cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to record a reading for session {SessionId}.", session.Id);
                return false;
            }
        })).ConfigureAwait(false);

        var recorded = results.Count(static ok => ok);
        if (recorded > 0)
            _logger.LogInformation("Recorded vitals for {Count} in-progress session(s).", recorded);
    }

    private static object NextReading(Guid sessionId)
    {
        // Random.Shared is thread-safe — required because a tick now records sessions concurrently.
        int Around(int mid, int spread) => mid + Random.Shared.Next(-spread, spread + 1);
        decimal AroundD(double mid, double spread) =>
            Math.Round((decimal)(mid + (((Random.Shared.NextDouble() * 2) - 1) * spread)), 1);

        // ~1 in 4 chairs is a deterministically-"critical" patient: its readings sit in the alert
        // band (hypertensive + tachycardic) so the chairside monitor demonstrates BOTH tones — the
        // steady moderate beep on normal chairs and the continuous critical alarm on this one. Keying
        // off the session id keeps a given chair in one mode instead of flapping every reading.
        var critical = (sessionId.GetHashCode() & 3) == 0;

        return new
        {
            systolicBloodPressure = critical ? Around(192, 8) : Around(120, 15),
            diastolicBloodPressure = critical ? Around(104, 6) : Around(75, 10),
            heartRateBpm = critical ? Around(132, 8) : Around(76, 12),
            arterialPressureMmHg = AroundD(-150, 20),
            venousPressureMmHg = critical ? AroundD(270, 15) : AroundD(150, 20),
            ultrafiltrationRateMlPerHour = AroundD(700, 120),
            conductivityMsPerCm = AroundD(13.8, 0.3),
            notes = (string?)null,
        };
    }
}
