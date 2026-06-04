using Dialysis.HIE.Persistence;
using Dialysis.HIE.Tefca.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIE.Composition.Demo;

/// <summary>
/// Development-only seeder that materialises a synthetic TEFCA QHIN partner the operator
/// can exercise end-to-end against the <c>/hie/admin/tefca/partners</c> admin page —
/// edit endpoints, attach a self-signed trust anchor, rotate an mTLS material, issue an
/// IAS JWT, transition Onboarding → Active.
///
/// The seeded partner intentionally stays in <c>Onboarding</c>; the operator drives the
/// activation invariants (≥ 1 trust anchor + mTLS material) by hand so the runbook in
/// <c>docs/identity/tefca-sandbox-runbook.md</c> covers a real activation flow. Real
/// QHIN credentials are not generated here — those come from the partner during a
/// production pilot.
///
/// Gated by <c>Hie:Demo:TefcaSandbox</c> (default <c>false</c>); the AppHost / module
/// composition opts in when the dev stack is running with the demo flag on.
/// </summary>
public sealed class HieTefcaSandboxSeeder : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<HieTefcaSandboxSeeder> _logger;
    /// <summary>
    /// Development-only seeder that materialises a synthetic TEFCA QHIN partner the operator
    /// can exercise end-to-end against the <c>/hie/admin/tefca/partners</c> admin page —
    /// edit endpoints, attach a self-signed trust anchor, rotate an mTLS material, issue an
    /// IAS JWT, transition Onboarding → Active.
    ///
    /// The seeded partner intentionally stays in <c>Onboarding</c>; the operator drives the
    /// activation invariants (≥ 1 trust anchor + mTLS material) by hand so the runbook in
    /// <c>docs/identity/tefca-sandbox-runbook.md</c> covers a real activation flow. Real
    /// QHIN credentials are not generated here — those come from the partner during a
    /// production pilot.
    ///
    /// Gated by <c>Hie:Demo:TefcaSandbox</c> (default <c>false</c>); the AppHost / module
    /// composition opts in when the dev stack is running with the demo flag on.
    /// </summary>
    public HieTefcaSandboxSeeder(IServiceProvider services,
        ILogger<HieTefcaSandboxSeeder> logger)
    {
        _services = services;
        _logger = logger;
    }

    // Stable id so the partner row is recognizable across restarts + redeploys. Uuid v7
    // for chronological-prefix ordering when the operator inspects the QhinPartners table.
    private static readonly Guid _sandboxPartnerId =
        new("0190a000-0000-7777-8000-000000000001");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HieDbContext>();
        if (!await db.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("HIE TEFCA sandbox seeder: database not reachable, skipping.");
            return;
        }

        if (await db.QhinPartners.AnyAsync(p => p.Id == _sandboxPartnerId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("HIE TEFCA sandbox seeder: sandbox partner already present, skipping.");
            return;
        }

        var partner = new QhinPartner(
            id: _sandboxPartnerId,
            name: "Acme Sandbox QHIN",
            // RFC 2606 .example domain so the URL is obviously synthetic + can't accidentally
            // hit real infrastructure if a developer drops it into a real FHIR client.
            fhirBaseUrl: "https://sandbox-qhin.example/fhir",
            iasEndpoint: "https://sandbox-qhin.example/ias",
            createdAtUtc: DateTime.UtcNow,
            updatedBy: "demo-seeder");

        db.QhinPartners.Add(partner);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "HIE TEFCA sandbox seeder: created sandbox partner {PartnerId} ({Name}). "
            + "Follow docs/identity/tefca-sandbox-runbook.md to drive it through trust-anchor "
            + "attach → mTLS rotation → Onboarding-to-Active.",
            partner.Id, partner.Name);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
