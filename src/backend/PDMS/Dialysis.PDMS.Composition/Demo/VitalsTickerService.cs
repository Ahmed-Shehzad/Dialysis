using Dialysis.PDMS.Persistence;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Realtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.PDMS.Composition.Demo;

/// <summary>
/// Development-only background service. Every <see cref="_interval"/>, walks every in-progress
/// dialysis session and records a synthetic reading, then broadcasts via the SignalR broadcaster
/// so the live-vitals SPA chart moves during demos without human input. Bypasses the CQRS
/// authorization pipeline by design — the ticker is system automation, not a user action.
///
/// Writes go directly to the <c>IntradialyticReadings</c> DbSet (not through the
/// <see cref="DialysisSession"/> aggregate's <c>RecordReading</c>). Mutating the eager-loaded
/// readings navigation triggers a spurious UPDATE on the parent row in EF Core's batch save
/// path that Npgsql reports as "0 rows affected", so every tick fails before any INSERT is
/// committed. Adding readings as their own DbSet entries sidesteps that change-tracking edge
/// case and keeps the ticker writing at 1Hz.
/// </summary>
public sealed class VitalsTickerService(IServiceProvider services, ILogger<VitalsTickerService> logger)
    : BackgroundService
{
    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(1);
    private static readonly Random _rng = new(8675309);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Vitals ticker started (every {Seconds}s).", _interval.TotalSeconds);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Vitals ticker tick failed.");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException) { return; }
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PdmsDbContext>();
        var broadcaster = scope.ServiceProvider.GetRequiredService<IVitalsBroadcaster>();
        var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        // Project to lightweight session ids — no tracking, no eager-loaded readings collection,
        // no change-tracker entry for the aggregate. Avoids the EF batch UPDATE side-effect that
        // tripped DbUpdateConcurrencyException when the ticker mutated the navigation.
        var activeSessionIds = await db.Sessions
            .AsNoTracking()
            .Where(s => s.Status == DialysisSessionStatus.InProgress)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (activeSessionIds.Count == 0) return;

        var now = time.GetUtcNow().UtcDateTime;
        var newReadings = new List<IntradialyticReading>(activeSessionIds.Count);

        foreach (var sessionId in activeSessionIds)
        {
            var reading = IntradialyticReading.Record(
                id: Guid.CreateVersion7(),
                sessionId: sessionId,
                observedAtUtc: now,
                systolic: 130 + _rng.Next(-15, 16),
                diastolic: 78 + _rng.Next(-8, 9),
                heartRateBpm: 72 + _rng.Next(-6, 7),
                arterialPressureMmHg: 175m + (decimal)_rng.NextDouble() * 20m,
                venousPressureMmHg: 145m + (decimal)_rng.NextDouble() * 20m,
                ultrafiltrationRateMlPerHour: 600m + (decimal)_rng.NextDouble() * 100m,
                conductivityMsPerCm: 14.0m + (decimal)_rng.NextDouble());
            db.Readings.Add(reading);
            newReadings.Add(reading);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var reading in newReadings)
        {
            await broadcaster.BroadcastAsync(
                new VitalsReadingSnapshot(
                    reading.Id, reading.SessionId, reading.ObservedAtUtc,
                    reading.SystolicBloodPressure, reading.DiastolicBloodPressure, reading.HeartRateBpm,
                    reading.ArterialPressureMmHg, reading.VenousPressureMmHg,
                    reading.UltrafiltrationRateMlPerHour, reading.ConductivityMsPerCm,
                    reading.Notes),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
