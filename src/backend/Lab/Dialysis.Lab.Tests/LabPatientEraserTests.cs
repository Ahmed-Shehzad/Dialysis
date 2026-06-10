using Dialysis.Lab.Contracts;
using Dialysis.Lab.Orders.Domain;
using Dialysis.Lab.Persistence;
using Dialysis.Lab.Persistence.Erasure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Dialysis.Lab.Tests;

/// <summary>
/// End-to-end smoke for the Lab contribution to the GDPR Art. 17 erasure pipeline. Exercises the
/// real <c>ExecuteUpdateAsync</c> path on Postgres (via the Testcontainer factory) so the
/// soft-delete shape is verified against the actual provider, not just an abstract DbContext.
/// </summary>
[Collection(nameof(LabFixtureCollection))]
public sealed class LabPatientEraserTests
{
    private readonly LabApiWebApplicationFactory _factory;

    /// <summary>
    /// End-to-end smoke for the Lab contribution to the GDPR Art. 17 erasure pipeline. Exercises the
    /// real <c>ExecuteUpdateAsync</c> path on Postgres (via the Testcontainer factory) so the
    /// soft-delete shape is verified against the actual provider, not just an abstract DbContext.
    /// </summary>
    public LabPatientEraserTests(LabApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Erase_Async_Soft_Deletes_Patient_Lab_Orders_And_Reports_The_Count_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
        var patientId = Guid.CreateVersion7();
        var otherPatient = Guid.CreateVersion7();
        var now = DateTime.UtcNow;

        // Seed three orders — two for the target patient, one for an unrelated patient
        // the eraser must leave alone.
        db.LabOrders.Add(New_Order(patientId, now));
        db.LabOrders.Add(New_Order(patientId, now));
        db.LabOrders.Add(New_Order(otherPatient, now));
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new LabPatientEraser(db, TimeProvider.System, NullLogger<LabPatientEraser>.Instance);

        var result = await sut.EraseAsync(patientId, "dpo@dialysis.test", CancellationToken.None);

        sut.ModuleSlug.ShouldBe("lab");
        result.RecordsErased.ShouldBe(2);
        result.ByCategory["LabOrder"].ShouldBe(2);

        // The two target orders are soft-deleted; the bystander is untouched.
        var rows = await db.LabOrders.AsNoTracking().ToListAsync(CancellationToken.None);
        var targeted = rows.Where(o => o.PatientId == patientId).ToList();
        targeted.Count.ShouldBe(2);
        targeted.ShouldAllBe(o => o.IsDeleted);
        targeted.ShouldAllBe(o => o.DeletedBy == "dpo@dialysis.test");
        targeted.ShouldAllBe(o => o.DeletedAt.HasValue);

        var bystander = rows.Single(o => o.PatientId == otherPatient);
        bystander.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Erase_Async_Is_Idempotent_Returning_Zero_On_A_Second_Pass_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
        var patientId = Guid.CreateVersion7();

        db.LabOrders.Add(New_Order(patientId, DateTime.UtcNow));
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new LabPatientEraser(db, TimeProvider.System, NullLogger<LabPatientEraser>.Instance);

        var first = await sut.EraseAsync(patientId, "dpo", CancellationToken.None);
        var second = await sut.EraseAsync(patientId, "dpo", CancellationToken.None);

        first.RecordsErased.ShouldBe(1);
        second.RecordsErased.ShouldBe(0);
        second.ByCategory.ShouldBeEmpty();
    }

    [Fact]
    public async Task Erase_Async_Returns_Zero_For_A_Patient_With_No_State_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
        var sut = new LabPatientEraser(db, TimeProvider.System, NullLogger<LabPatientEraser>.Instance);

        var result = await sut.EraseAsync(Guid.CreateVersion7(), "dpo", CancellationToken.None);

        result.RecordsErased.ShouldBe(0);
        result.ByCategory.ShouldBeEmpty();
    }

    private static LabOrder New_Order(Guid patientId, DateTime nowUtc)
    {
        // Fresh LabTestItem instances per order: the items are EF-owned entities tracked by
        // reference, so a shared instance would silently re-parent to the last order added.
        var order = LabOrder.Place(
            patientId,
            [new LabTestItem("718-7", "Hemoglobin"), new LabTestItem("2160-0", "Creatinine")],
            LabOrderPriority.Routine, "Serum", "dr.house", nowUtc);
        // The seeded aggregate raised LabOrderPlacedIntegrationEvent; tests persist state only.
        order.ClearIntegrationEvents();
        return order;
    }
}
