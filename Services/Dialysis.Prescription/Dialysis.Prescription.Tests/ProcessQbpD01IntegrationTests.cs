using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Dialysis.Prescription.Application.Domain;
using Dialysis.Prescription.Application.Features.ProcessQbpD01Query;
using Dialysis.Prescription.Infrastructure.Hl7;
using Dialysis.Prescription.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace Dialysis.Prescription.Tests;

/// <summary>
/// Integration test: QBP^D01 → ProcessQbpD01QueryCommandHandler → RSP^K22 round-trip.
/// Uses in-memory EF and real parser, builder, repository.
/// </summary>
public sealed class ProcessQbpD01IntegrationTests
{
    private const string QbpD01WithMrn = """
                                         MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||QBP^D01^QBP_D01|MSG001|P|2.6
                                         QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q001|@PID.3|MRN123^^^^MR
                                         RCP|I||RD
                                         """;

    [Fact]
    public async Task RoundTrip_PrescriptionExists_ReturnsRspK22WithPrescriptionAsync()
    {
        DbContextOptions<PrescriptionDbContext> options = new DbContextOptionsBuilder<PrescriptionDbContext>()
            .UseInMemoryDatabase(databaseName: "QbpD01_Integration_" + Guid.NewGuid())
            .Options;

        await using var db = new PrescriptionDbContext(options);
        _ = await db.Database.EnsureCreatedAsync();

        var prescription = Dialysis.Prescription.Application.Domain.Prescription.Create(
            "ORD001",
            new MedicalRecordNumber("MRN123"),
            "HD",
            "DR_SMITH",
            "555-1234");

        prescription.AddSetting(ProfileSetting.Constant("MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING", 300, null, "RSET"));
        prescription.AddSetting(ProfileSetting.Constant("MDC_HDIALY_UF_RATE_SETTING", 500, null, "RSET"));
        prescription.AddSetting(ProfileSetting.Constant("MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE", 2000, null, "RSET"));

        _ = db.Prescriptions.Add(prescription);
        int saved = await db.SaveChangesAsync();
        saved.ShouldBeGreaterThan(0);

        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new PrescriptionRepository(db, tenant);
        var parser = new QbpD01Parser();
        var builder = new RspK22Builder();

        var handler = new ProcessQbpD01QueryCommandHandler(parser, builder, repository);
        var command = new ProcessQbpD01QueryCommand(QbpD01WithMrn);

        ProcessQbpD01QueryResponse response = await handler.HandleAsync(command);

        response.Mrn.ShouldBe("MRN123");
        response.RspK22Message.ShouldContain("MSH|");
        response.RspK22Message.ShouldContain("MSA|AA|");
        response.RspK22Message.ShouldContain("RSP^K22");
        response.RspK22Message.ShouldContain("QAK|");
        response.RspK22Message.ShouldContain("ORC|");
        response.RspK22Message.ShouldContain("PID|");
        response.RspK22Message.ShouldContain("OBX|");
        response.RspK22Message.ShouldContain("300");
        response.RspK22Message.ShouldContain("500");
        response.RspK22Message.ShouldContain("2000");
        response.RspK22Message.ShouldContain("Q001|OK");
    }

    [Fact]
    public async Task RoundTrip_NoPrescription_ReturnsRspK22WithNfAsync()
    {
        DbContextOptions<PrescriptionDbContext> options = new DbContextOptionsBuilder<PrescriptionDbContext>()
            .UseInMemoryDatabase(databaseName: "QbpD01_NoRx_" + Guid.NewGuid())
            .Options;

        await using var db = new PrescriptionDbContext(options);
        _ = await db.Database.EnsureCreatedAsync();

        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new PrescriptionRepository(db, tenant);
        var parser = new QbpD01Parser();
        var builder = new RspK22Builder();

        var handler = new ProcessQbpD01QueryCommandHandler(parser, builder, repository);
        var command = new ProcessQbpD01QueryCommand(QbpD01WithMrn);

        ProcessQbpD01QueryResponse response = await handler.HandleAsync(command);

        response.Mrn.ShouldBe("MRN123");
        response.RspK22Message.ShouldContain("MSA|AA|");
        response.RspK22Message.ShouldContain("QAK|");
        response.RspK22Message.ShouldContain("NF");
        response.RspK22Message.ShouldNotContain("ORC|");
        response.RspK22Message.ShouldNotContain("OBX|");
    }
}
