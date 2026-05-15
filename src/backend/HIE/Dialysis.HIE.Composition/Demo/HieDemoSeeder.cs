using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIE.Composition.Demo;

/// <summary>
/// Development-only seeder. Creates a handful of cross-organization consent grants so the SPA's
/// FHIR Exchange page has visible data on load. Idempotent.
/// </summary>
public sealed class HieDemoSeeder(IServiceProvider services, ILogger<HieDemoSeeder> logger) : IHostedService
{
    private static readonly (Guid PatientId, string PartnerId, string Scope, ConsentDirection Direction)[] _Demo =
    [
        (new Guid("00000000-0000-0000-0000-000000000001"), "partner.cleveland", "fhir/Observation.read", ConsentDirection.Outbound),
        (new Guid("00000000-0000-0000-0000-000000000001"), "partner.mayo",      "fhir/Encounter.read",   ConsentDirection.Outbound),
        (new Guid("00000000-0000-0000-0000-000000000002"), "partner.mayo",      "fhir/Patient.read",     ConsentDirection.Inbound),
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HieDbContext>();
        if (!await db.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogWarning("HIE demo seeder: database not reachable, skipping.");
            return;
        }
        try
        {
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "HIE demo seeder: migrations failed, attempting seed on existing schema.");
        }

        if (await db.Consents.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("HIE demo seeder: consents already present, skipping.");
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var (patientId, partner, scope_, direction) in _Demo)
        {
            db.Consents.Add(new ConsentRecord(patientId, partner, scope_, direction, now.AddDays(-30), now.AddDays(180)));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("HIE demo seeder: seeded {Count} consent grants.", _Demo.Length);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
