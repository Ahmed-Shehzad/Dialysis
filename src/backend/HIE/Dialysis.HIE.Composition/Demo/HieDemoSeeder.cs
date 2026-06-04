using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Persistence;
using Dialysis.Module.Contracts.Demo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIE.Composition.Demo;

/// <summary>
/// Development-only seeder. Creates a handful of cross-organization consent grants so the SPA's
/// FHIR Exchange page has visible data on load. Idempotent.
/// </summary>
public sealed class HieDemoSeeder : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<HieDemoSeeder> _logger;
    /// <summary>
    /// Development-only seeder. Creates a handful of cross-organization consent grants so the SPA's
    /// FHIR Exchange page has visible data on load. Idempotent.
    /// </summary>
    public HieDemoSeeder(IServiceProvider services, ILogger<HieDemoSeeder> logger)
    {
        _services = services;
        _logger = logger;
    }

    // Patient ids come from the cross-module DemoDataCatalog so these consents attach to the same
    // patients EHR/PDMS/HIS seed — the HIE FHIR-Exchange page lines up with the rest of the demo.
    private static readonly (Guid PatientId, string PartnerId, string Scope, ConsentDirection Direction)[] _demo =
    [
        (DemoDataCatalog.Patients[0].Id, DemoDataCatalog.PartnerCleveland, "fhir/Observation.read", ConsentDirection.Outbound),
        (DemoDataCatalog.Patients[0].Id, DemoDataCatalog.PartnerMayo,      "fhir/Encounter.read",   ConsentDirection.Outbound),
        (DemoDataCatalog.Patients[1].Id, DemoDataCatalog.PartnerMayo,      "fhir/Patient.read",     ConsentDirection.Inbound),
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var serviceScope = _services.CreateScope();
        var db = serviceScope.ServiceProvider.GetRequiredService<HieDbContext>();
        if (!await db.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("HIE demo seeder: database not reachable, skipping.");
            return;
        }
        try
        {
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HIE demo seeder: migrations failed, attempting seed on existing schema.");
        }

        if (await db.Consents.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("HIE demo seeder: consents already present, skipping.");
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var (patientId, partner, scope, direction) in _demo)
        {
            db.Consents.Add(new ConsentRecord(patientId, partner, scope, direction, now.AddDays(-30), now.AddDays(180)));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("HIE demo seeder: seeded {Count} consent grants.", _demo.Length);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
