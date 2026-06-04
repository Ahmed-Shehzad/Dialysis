using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Dialysis.HIS.Persistence;
using Dialysis.HIS.Persistence.Erasure;
using Dialysis.HIS.Scheduling.Domain;
using Dialysis.HIS.Scheduling.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Dialysis.HIS.Tests;

/// <summary>
/// End-to-end smoke for the HIS contribution to the GDPR Art. 17 erasure pipeline. Exercises the
/// real <c>ExecuteUpdateAsync</c> path on Postgres (via the Testcontainer factory) so the
/// soft-delete shadow-column shape is verified against the actual provider, not just an
/// abstract DbContext.
/// </summary>
[Collection(nameof(HisFixtureCollection))]
public sealed class HisPatientEraserTests
{
    private readonly HisApiWebApplicationFactory _factory;
    /// <summary>
    /// End-to-end smoke for the HIS contribution to the GDPR Art. 17 erasure pipeline. Exercises the
    /// real <c>ExecuteUpdateAsync</c> path on Postgres (via the Testcontainer factory) so the
    /// soft-delete shadow-column shape is verified against the actual provider, not just an
    /// abstract DbContext.
    /// </summary>
    public HisPatientEraserTests(HisApiWebApplicationFactory factory) => _factory = factory;
    [Fact]
    public async Task Erase_Async_Soft_Deletes_Patient_Appointments_And_Reports_The_Count_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();
        var patientId = Guid.CreateVersion7();
        var otherPatient = Guid.CreateVersion7();
        var providerId = Guid.CreateVersion7();
        var now = DateTime.UtcNow;

        // Seed three appointments — two for the target patient, one for an unrelated patient
        // we expect the eraser to leave alone.
        db.Appointments.Add(Appointment.Book(patientId, providerId, new AppointmentSlot(now.AddDays(1), now.AddDays(1).AddHours(1)), now));
        db.Appointments.Add(Appointment.Book(patientId, providerId, new AppointmentSlot(now.AddDays(2), now.AddDays(2).AddHours(1)), now));
        db.Appointments.Add(Appointment.Book(otherPatient, providerId, new AppointmentSlot(now.AddDays(3), now.AddDays(3).AddHours(1)), now));
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new HisPatientEraser(db, TimeProvider.System, NullLogger<HisPatientEraser>.Instance);

        var result = await sut.EraseAsync(patientId, "dpo@dialysis.test", CancellationToken.None);

        result.RecordsErased.ShouldBe(2);
        result.ByCategory["Appointment"].ShouldBe(2);
        result.ModuleSlugShouldBeHis(sut);

        // The two target appointments are soft-deleted; the third is untouched.
        var rows = await db.Appointments.AsNoTracking().ToListAsync(CancellationToken.None);
        var targeted = rows.Where(a => a.PatientId == patientId).ToList();
        targeted.Count.ShouldBe(2);
        targeted.ShouldAllBe(a => a.IsDeleted);
        targeted.ShouldAllBe(a => a.DeletedBy == "dpo@dialysis.test");
        targeted.ShouldAllBe(a => a.DeletedAt.HasValue);

        var bystander = rows.Single(a => a.PatientId == otherPatient);
        bystander.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Erase_Async_Is_Idempotent_Returning_Zero_On_A_Second_Pass_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();
        var patientId = Guid.CreateVersion7();
        var providerId = Guid.CreateVersion7();
        var now = DateTime.UtcNow;

        db.Appointments.Add(Appointment.Book(patientId, providerId, new AppointmentSlot(now.AddDays(1), now.AddDays(1).AddHours(1)), now));
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new HisPatientEraser(db, TimeProvider.System, NullLogger<HisPatientEraser>.Instance);

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
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();
        var sut = new HisPatientEraser(db, TimeProvider.System, NullLogger<HisPatientEraser>.Instance);

        var result = await sut.EraseAsync(Guid.CreateVersion7(), "dpo", CancellationToken.None);

        result.RecordsErased.ShouldBe(0);
        result.ByCategory.ShouldBeEmpty();
    }
}

file static class EraserExtensions
{
    public static void ModuleSlugShouldBeHis(this PatientErasureResult _, IPatientEraser eraser)
        => eraser.ModuleSlug.ShouldBe("his");
}
