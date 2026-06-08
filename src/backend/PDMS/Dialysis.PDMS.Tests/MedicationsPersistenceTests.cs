using Dialysis.PDMS.Api.Controllers.V1;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.Medications.Domain;
using Dialysis.PDMS.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests;

/// <summary>
/// Integration coverage for the chairside MAR write path against a real Postgres Testcontainer.
/// Guards the regression where <see cref="MedicationsController"/> tracked Add/Update but never
/// called SaveChanges (so writes were silently dropped — the in-memory repo masked it by
/// persisting on Add), and the idempotent lazy MAR open against the unique SessionId index.
/// </summary>
[Collection(nameof(PdmsPostgresFixtureCollection))]
public sealed class MedicationsPersistenceTests
{
    private readonly PdmsApiWebApplicationFactory _factory;
    public MedicationsPersistenceTests(PdmsApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Recorded_Administration_Persists_To_Postgres_Async()
    {
        var sessionId = Guid.CreateVersion7();
        var patientId = Guid.CreateVersion7();

        await Record_Administration_Async(sessionId, patientId, "ondansetron");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PdmsDbContext>();
        var mar = await db.Set<MedicationAdministrationRecord>().AsNoTracking()
            .FirstOrDefaultAsync(m => m.SessionId == sessionId, CancellationToken.None);

        mar.ShouldNotBeNull();
        mar.Entries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Repeated_Administrations_Reuse_One_Mar_Async()
    {
        var sessionId = Guid.CreateVersion7();
        var patientId = Guid.CreateVersion7();

        // Two writes against the same session: the second must find the existing MAR (no second
        // insert against the unique SessionId index) and append its entry.
        await Record_Administration_Async(sessionId, patientId, "ondansetron");
        await Record_Administration_Async(sessionId, patientId, "heparin");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PdmsDbContext>();
        var mars = await db.Set<MedicationAdministrationRecord>().AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .ToListAsync(CancellationToken.None);

        mars.Count.ShouldBe(1);
        mars[0].Entries.Count.ShouldBe(2);
    }

    // Each call gets its own scope (its own DbContext + repository), so the controller exercises the
    // real EF SaveChanges path end-to-end rather than a single warm tracking graph.
    private async Task Record_Administration_Async(Guid sessionId, Guid patientId, string code)
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var controller = new MedicationsController(
            sp.GetRequiredService<IPdmsRepository<MedicationAdministrationRecord, Guid>>(),
            sp.GetRequiredService<PdmsDbContext>(),
            TimeProvider.System);

        var result = await controller.RecordAdministrationAsync(
            sessionId,
            new RecordAdministrationRequest(
                PatientId: patientId,
                CodeSystem: "http://www.nlm.nih.gov/research/umls/rxnorm",
                Code: code,
                Display: code,
                DoseQuantity: 4m,
                DoseUnit: "mg",
                Route: "Intravenous",
                AdministeredBySub: "nurse-1"),
            CancellationToken.None);

        result.ShouldBeOfType<CreatedResult>();
    }
}
