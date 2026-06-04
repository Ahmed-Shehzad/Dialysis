using Dialysis.Module.Contracts.Demo;
using Dialysis.PDMS.Persistence;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.PDMS.Composition.Demo;

/// <summary>
/// Development-only seeder that paints a <em>multi-state snapshot</em> of dialysis sessions keyed
/// by the cross-module <see cref="DemoDataCatalog"/> patients, so every chairside / sessions
/// screen is populated the instant the demo loads:
/// <list type="bullet">
///   <item>one <b>in-progress</b> session (live vitals + live cost ticking),</item>
///   <item>one <b>paused</b> session (usage timer + cost frozen),</item>
///   <item>two <b>scheduled</b> sessions reserved for the presenter to drive live
///         (Start → Pause → Complete → invoice) on stage.</item>
/// </list>
/// Completed sessions + invoices are produced continuously by
/// <see cref="SessionLifecycleSimulator"/> (which runs after every consumer is bound), so the
/// completion→billing→invoice→document pipeline is exercised for real rather than fabricated.
/// Idempotent: skips entirely once any session exists (reset via the demo admin endpoint).
/// </summary>
public sealed class PdmsDemoSeeder : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PdmsDemoSeeder> _logger;

    /// <summary>Creates the seeder.</summary>
    public PdmsDemoSeeder(IServiceProvider services, ILogger<PdmsDemoSeeder> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PdmsDbContext>();
        if (!await db.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("PDMS demo seeder: database not reachable, skipping.");
            return;
        }
        try
        {
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDMS demo seeder: migrations failed, attempting seed on existing schema.");
        }

        if (await db.Sessions.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("PDMS demo seeder: sessions already present, skipping.");
            return;
        }

        var repo = scope.ServiceProvider.GetRequiredService<IDialysisSessionRepository>();
        var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        await SeedSnapshotAsync(repo, db, time, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "PDMS demo seeder: seeded snapshot (1 in-progress, 1 paused, 2 scheduled).");
    }

    /// <summary>
    /// Paints the multi-state snapshot (1 in-progress, 1 paused, 2 scheduled) into the session
    /// store and saves. Shared by the startup seeder and the demo reset endpoint. Assumes the
    /// caller has already cleared any existing sessions.
    /// </summary>
    public static async Task SeedSnapshotAsync(
        IDialysisSessionRepository repo, PdmsDbContext db, TimeProvider time, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var patients = DemoDataCatalog.Patients;

        // Patient 0 — in-progress for ~45 min: the live-cost tile ticks and the vitals ticker
        // keeps adding readings. (Schedule() rejects start times > 1h in the past, so we keep
        // the scheduled time inside that window and back-date the actual start to it.)
        var inProgress = NewScheduledSession(patients[0].Id, now.AddMinutes(-45));
        inProgress.Start(now.AddMinutes(-45));
        SeedReadings(inProgress, now.AddMinutes(-45), count: 8, stepMinutes: 5);
        repo.Add(inProgress);

        // Patient 1 — paused: ran 30 min then paused 20 min ago, so the usage timer freezes at
        // 30 min and the live cost holds steady (machine off) — the pause-aware accounting demo.
        var paused = NewScheduledSession(patients[1].Id, now.AddMinutes(-50));
        paused.Start(now.AddMinutes(-50));
        SeedReadings(paused, now.AddMinutes(-50), count: 6, stepMinutes: 5);
        paused.Pause(now.AddMinutes(-20));
        repo.Add(paused);

        // Patients 3 & 4 — scheduled, reserved for the presenter to drive live on stage. The
        // autopilot never touches these (it only manages sessions it creates itself).
        repo.Add(NewScheduledSession(patients[3].Id, now.AddMinutes(10)));
        repo.Add(NewScheduledSession(patients[4].Id, now.AddMinutes(25)));

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Builds a scheduled HD session with the standard demo prescription + access.</summary>
    internal static DialysisSession NewScheduledSession(Guid patientId, DateTime scheduledStartUtc)
    {
        var prescription = new SessionPrescription(
            dialyzerModel: "Fresenius FX-80",
            prescribedDurationMinutes: 240,
            bloodFlowRateMlPerMin: 350,
            dialysateFlowRateMlPerMin: 500,
            dialysatePotassiumMmolPerL: 2.0m,
            dialysateCalciumMmolPerL: 1.5m,
            dialysateSodiumMmolPerL: 138.0m,
            targetUfVolumeLiters: 2.5m,
            anticoagulationProtocolCode: "HEPARIN");

        var access = new VascularAccess(
            kind: VascularAccessKind.ArteriovenousFistula,
            site: "Left forearm",
            establishedOn: DateOnly.FromDateTime(scheduledStartUtc.AddYears(-1)));

        return DialysisSession.Schedule(
            id: Guid.CreateVersion7(),
            patientId: patientId,
            scheduledStartUtc: scheduledStartUtc,
            prescription: prescription,
            access: access);
    }

    /// <summary>Records <paramref name="count"/> plausible intradialytic readings from the start time.</summary>
    internal static void SeedReadings(DialysisSession session, DateTime fromUtc, int count, double stepMinutes)
    {
        for (var i = 0; i < count; i++)
        {
            session.RecordReading(
                fromUtc.AddMinutes(i * stepMinutes),
                systolic: 138 + (i % 5),
                diastolic: 82 + (i % 3),
                heartRateBpm: 72 + (i % 6),
                arterialPressureMmHg: 180 + (i % 4),
                venousPressureMmHg: 148 + (i % 5),
                ultrafiltrationRateMlPerHour: 600,
                conductivityMsPerCm: 14.0m);
        }
    }
}
