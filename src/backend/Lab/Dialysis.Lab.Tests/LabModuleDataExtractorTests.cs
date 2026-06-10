using Dialysis.Lab.Contracts;
using Dialysis.Lab.Orders.Domain;
using Dialysis.Lab.Persistence;
using Dialysis.Lab.Persistence.DataSubjectRights;
using Dialysis.Lab.Persistence.Erasure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Dialysis.Lab.Tests;

/// <summary>
/// End-to-end smoke for the Lab contribution to the GDPR Art. 15 / 20 export pipeline. Verifies
/// the extractor mirrors the eraser: same rows in scope (patient-scoped, live only), read into
/// <c>DataSubjectResource</c> entries instead of deleted.
/// </summary>
[Collection(nameof(LabFixtureCollection))]
public sealed class LabModuleDataExtractorTests
{
    private readonly LabApiWebApplicationFactory _factory;

    /// <summary>
    /// End-to-end smoke for the Lab contribution to the GDPR Art. 15 / 20 export pipeline. Verifies
    /// the extractor mirrors the eraser: same rows in scope (patient-scoped, live only), read into
    /// <c>DataSubjectResource</c> entries instead of deleted.
    /// </summary>
    public LabModuleDataExtractorTests(LabApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Extract_Async_Returns_Only_The_Target_Patients_Orders_As_Json_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
        var patientId = Guid.CreateVersion7();
        var otherPatient = Guid.CreateVersion7();
        var now = DateTime.UtcNow;

        var mine = New_Order(patientId, now);
        db.LabOrders.Add(mine);
        db.LabOrders.Add(New_Order(patientId, now));
        db.LabOrders.Add(New_Order(otherPatient, now));
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new LabModuleDataExtractor(db);

        var resources = await sut.ExtractAsync(patientId, CancellationToken.None);

        sut.ModuleSlug.ShouldBe("lab");
        resources.Count.ShouldBe(2);
        resources.ShouldAllBe(r => r.ResourceType == "LabOrder");
        resources.Select(r => r.Identifier).ShouldContain(mine.Id.ToString());

        // The serialized row carries the order's personal data in camelCase JSON, including the
        // LOINC-coded requested tests that ride inline on the row.
        var exported = resources.Single(r => r.Identifier == mine.Id.ToString());
        exported.Json.ShouldContain(mine.PlacerOrderNumber);
        exported.Json.ShouldContain($"\"patientId\":\"{patientId}\"");
        exported.Json.ShouldContain("718-7");
    }

    [Fact]
    public async Task Extract_Async_Excludes_Rows_The_Eraser_Already_Soft_Deleted_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
        var patientId = Guid.CreateVersion7();

        db.LabOrders.Add(New_Order(patientId, DateTime.UtcNow));
        await db.SaveChangesAsync(CancellationToken.None);

        var eraser = new LabPatientEraser(db, TimeProvider.System, NullLogger<LabPatientEraser>.Instance);
        await eraser.EraseAsync(patientId, "dpo", CancellationToken.None);

        var sut = new LabModuleDataExtractor(db);

        var resources = await sut.ExtractAsync(patientId, CancellationToken.None);

        resources.ShouldBeEmpty();
    }

    [Fact]
    public async Task Extract_Async_Returns_Empty_For_A_Patient_With_No_State_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
        var sut = new LabModuleDataExtractor(db);

        var resources = await sut.ExtractAsync(Guid.CreateVersion7(), CancellationToken.None);

        resources.ShouldBeEmpty();
    }

    private static LabOrder New_Order(Guid patientId, DateTime nowUtc)
    {
        // Fresh LabTestItem instances per order: the items are EF-owned entities tracked by
        // reference, so a shared instance would silently re-parent to the last order added.
        var order = LabOrder.Place(
            patientId,
            [new LabTestItem("718-7", "Hemoglobin")],
            LabOrderPriority.Routine, "Serum", "dr.house", nowUtc);
        // The seeded aggregate raised LabOrderPlacedIntegrationEvent; tests persist state only.
        order.ClearIntegrationEvents();
        return order;
    }
}
