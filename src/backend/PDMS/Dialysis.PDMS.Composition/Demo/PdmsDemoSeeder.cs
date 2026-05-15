using Dialysis.PDMS.Persistence;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Dialysis.PDMS.TreatmentSessions.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.PDMS.Composition.Demo;

/// <summary>
/// Development-only seeder that ensures a small set of in-progress dialysis sessions exists
/// keyed by the EHR demo patient slots (offsets 0..4 in the EHR demo MRN list). Idempotent.
/// </summary>
public sealed class PdmsDemoSeeder(IServiceProvider services, ILogger<PdmsDemoSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PdmsDbContext>();
        if (!await db.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogWarning("PDMS demo seeder: database not reachable, skipping.");
            return;
        }
        try
        {
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PDMS demo seeder: migrations failed, attempting seed on existing schema.");
        }

        if (await db.Sessions.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("PDMS demo seeder: sessions already present, skipping.");
            return;
        }

        var repo = scope.ServiceProvider.GetRequiredService<IDialysisSessionRepository>();
        var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var now = time.GetUtcNow().UtcDateTime;

        // Stable demo patient ids — these must match what the EHR seeder produces if the demo
        // is run cross-module, but for a standalone PDMS demo any deterministic Guid set works.
        var demoPatientIds = Enumerable.Range(1, 3)
            .Select(i => new Guid($"00000000-0000-0000-0000-0000000000{i:D2}"))
            .ToArray();

        foreach (var patientId in demoPatientIds)
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
                establishedOn: DateOnly.FromDateTime(now.AddYears(-1)));

            var session = DialysisSession.Schedule(
                id: Guid.CreateVersion7(),
                patientId: patientId,
                scheduledStartUtc: now.AddMinutes(-15),
                prescription: prescription,
                access: access);
            session.Start(now.AddMinutes(-15));

            // Seed a handful of initial readings so the chart isn't empty before the ticker fires.
            for (var i = 0; i < 4; i++)
            {
                session.RecordReading(
                    now.AddMinutes(-15 + i * 3),
                    systolic: 138 + i,
                    diastolic: 82 + (i % 2),
                    heartRateBpm: 74 + i,
                    arterialPressureMmHg: 180,
                    venousPressureMmHg: 150,
                    ultrafiltrationRateMlPerHour: 600,
                    conductivityMsPerCm: 14.0m);
            }

            repo.Add(session);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("PDMS demo seeder: seeded {Count} in-progress sessions.", demoPatientIds.Length);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
