using Dialysis.HIS.Persistence;
using Dialysis.HIS.Persistence.DataSubjectRights;
using Dialysis.HIS.Persistence.Erasure;
using Dialysis.HIS.Scheduling.Domain;
using Dialysis.HIS.Scheduling.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Dialysis.HIS.Tests;

/// <summary>
/// End-to-end smoke for the HIS contribution to the GDPR Art. 15 / 20 export pipeline. Verifies
/// the extractor mirrors <see cref="HisPatientEraser"/>: the same patient-scoped rows, read into
/// <c>DataSubjectResource</c> entries instead of deleted, and excluding rows the eraser already
/// soft-deleted.
/// </summary>
[Collection(nameof(HisFixtureCollection))]
public sealed class HisModuleDataExtractorTests
{
    private readonly HisApiWebApplicationFactory _factory;

    /// <summary>
    /// End-to-end smoke for the HIS contribution to the GDPR Art. 15 / 20 export pipeline. Verifies
    /// the extractor mirrors <see cref="HisPatientEraser"/>: the same patient-scoped rows, read into
    /// <c>DataSubjectResource</c> entries instead of deleted, and excluding rows the eraser already
    /// soft-deleted.
    /// </summary>
    public HisModuleDataExtractorTests(HisApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Extract_Async_Returns_Only_The_Target_Patients_Rows_As_Json_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();
        var patientId = Guid.CreateVersion7();
        var otherPatient = Guid.CreateVersion7();
        var providerId = Guid.CreateVersion7();
        var now = DateTime.UtcNow;

        var mine = Appointment.Book(patientId, providerId, new AppointmentSlot(now.AddDays(1), now.AddDays(1).AddHours(1)), now);
        db.Appointments.Add(mine);
        db.Appointments.Add(Appointment.Book(patientId, providerId, new AppointmentSlot(now.AddDays(2), now.AddDays(2).AddHours(1)), now));
        db.Appointments.Add(Appointment.Book(otherPatient, providerId, new AppointmentSlot(now.AddDays(3), now.AddDays(3).AddHours(1)), now));
        await db.SaveChangesAsync(CancellationToken.None);

        var sut = new HisModuleDataExtractor(db);

        var resources = await sut.ExtractAsync(patientId, CancellationToken.None);

        sut.ModuleSlug.ShouldBe("his");
        var appointments = resources.Where(r => r.ResourceType == "Appointment").ToList();
        appointments.Count.ShouldBe(2);
        appointments.Select(r => r.Identifier).ShouldContain(mine.Id.ToString());
        appointments.ShouldAllBe(r => r.Json.Contains($"\"patientId\":\"{patientId}\""));
    }

    [Fact]
    public async Task Extract_Async_Excludes_Rows_The_Eraser_Already_Soft_Deleted_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();
        var patientId = Guid.CreateVersion7();
        var providerId = Guid.CreateVersion7();
        var now = DateTime.UtcNow;

        db.Appointments.Add(Appointment.Book(patientId, providerId, new AppointmentSlot(now.AddDays(1), now.AddDays(1).AddHours(1)), now));
        await db.SaveChangesAsync(CancellationToken.None);

        var eraser = new HisPatientEraser(db, TimeProvider.System, NullLogger<HisPatientEraser>.Instance);
        await eraser.EraseAsync(patientId, "dpo", CancellationToken.None);

        var sut = new HisModuleDataExtractor(db);

        var resources = await sut.ExtractAsync(patientId, CancellationToken.None);

        resources.ShouldBeEmpty();
    }

    [Fact]
    public async Task Extract_Async_Returns_Empty_For_A_Patient_With_No_State_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();
        var sut = new HisModuleDataExtractor(db);

        var resources = await sut.ExtractAsync(Guid.CreateVersion7(), CancellationToken.None);

        resources.ShouldBeEmpty();
    }
}
