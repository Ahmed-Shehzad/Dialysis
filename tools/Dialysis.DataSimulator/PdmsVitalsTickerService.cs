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

    // Per-session deterministic jitter source keyed off the base seed; values wander around clinical
    // baselines so the chairside trend looks alive rather than flat-lining at a constant.
    private readonly Random _random;

    /// <summary>Creates the ticker.</summary>
    public PdmsVitalsTickerService(
        IServiceProvider services,
        IOptions<DataSimulatorOptions> options,
        ILogger<PdmsVitalsTickerService> logger)
    {
        _services = services;
        _options = options.Value;
        _logger = logger;
        _random = new Random(_options.Seed);
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

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var pdms = scope.ServiceProvider.GetRequiredService<IPdmsClient>();

        var sessions = await pdms.ListInProgressSessionsAsync(cancellationToken).ConfigureAwait(false);
        if (sessions.Count == 0)
            return;

        var recorded = 0;
        foreach (var session in sessions)
        {
            try
            {
                await pdms.RecordReadingAsync(session.Id, NextReading(), cancellationToken).ConfigureAwait(false);
                recorded++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to record a reading for session {SessionId}.", session.Id);
            }
        }

        if (recorded > 0)
            _logger.LogInformation("Recorded vitals for {Count} in-progress session(s).", recorded);
    }

    private object NextReading()
    {
        int Around(int mid, int spread) => mid + _random.Next(-spread, spread + 1);
        decimal AroundD(double mid, double spread) =>
            Math.Round((decimal)(mid + ((_random.NextDouble() * 2 - 1) * spread)), 1);

        return new
        {
            systolicBloodPressure = Around(120, 15),
            diastolicBloodPressure = Around(75, 10),
            heartRateBpm = Around(76, 12),
            arterialPressureMmHg = AroundD(-150, 20),
            venousPressureMmHg = AroundD(150, 20),
            ultrafiltrationRateMlPerHour = AroundD(700, 120),
            conductivityMsPerCm = AroundD(13.8, 0.3),
            notes = (string?)null,
        };
    }
}
