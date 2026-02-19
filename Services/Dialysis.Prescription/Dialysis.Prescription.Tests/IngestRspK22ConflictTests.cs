using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Dialysis.Prescription.Application.Domain;
using Dialysis.Prescription.Application.Exceptions;
using Dialysis.Prescription.Application.Features.IngestRspK22Message;
using Dialysis.Prescription.Infrastructure.Hl7;
using Dialysis.Prescription.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Shouldly;

using PrescriptionEntity = Dialysis.Prescription.Application.Domain.Prescription;

namespace Dialysis.Prescription.Tests;

/// <summary>
/// Integration tests for prescription conflict handling: Callback (409 with callbackPhone), Partial merge.
/// </summary>
public sealed class IngestRspK22ConflictTests
{
    private const string RspK22Order001 = """
                                           MSH|^~\&|EMR|FAC|MACH|FAC|20230215120000||RSP^K22^RSP_K21|MSG001|P|2.6
                                           MSA|AA|MSG001
                                           QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q001|@PID.3|MRN123^^^^MR
                                           ORC|NW|ORD001^FAC|||||20230215120000|||PROVIDER||555-1111
                                           PID|||MRN123^^^^MR
                                           OBX|1|NM|12345^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING^MDC||300|ml/min||||||||||RSET
                                           OBX|2|NM|12346^MDC_HDIALY_UF_RATE_SETTING^MDC||500|mL/h||||||||||RSET
                                           OBX|3|NM|12347^MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE^MDC||2000|mL||||||||||RSET
                                           """;

    private const string RspK22Order001WithNewSetting = """
                                                        MSH|^~\&|EMR|FAC|MACH|FAC|20230215130000||RSP^K22^RSP_K21|MSG002|P|2.6
                                                        MSA|AA|MSG002
                                                        QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q002|@PID.3|MRN123^^^^MR
                                                        ORC|NW|ORD001^FAC|||||20230215130000|||PROVIDER||555-2222
                                                        PID|||MRN123^^^^MR
                                                        OBX|1|NM|12345^MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING^MDC||350|ml/min||||||||||RSET
                                                        OBX|2|NM|12346^MDC_HDIALY_UF_RATE_SETTING^MDC||600|mL/h||||||||||RSET
                                                        OBX|3|NM|12347^MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE^MDC||2500|mL||||||||||RSET
                                                        OBX|4|NM|12348^MDC_HDIALY_DIALYSATE_FLOW_RATE_SETTING^MDC||500|ml/min||||||||||RSET
                                                        """;

    [Fact]
    public async Task Callback_WhenConflict_ThrowsPrescriptionConflictExceptionWithCallbackPhoneAsync()
    {
        var (db, handler) = await CreateDbAndHandlerAsync();
        await using (db)
        {
            var command = new IngestRspK22MessageCommand(RspK22Order001, null, PrescriptionConflictPolicy.Callback);

            PrescriptionConflictException ex = await Should.ThrowAsync<PrescriptionConflictException>(
                () => handler.HandleAsync(command));

            ex.OrderId.ShouldBe("ORD001");
            ex.CallbackPhone.ShouldBe("555-1111");
        }
    }

    [Fact]
    public async Task Partial_WhenConflict_MergesNewSettingsOnlyAsync()
    {
        var (db, handler) = await CreateDbAndHandlerAsync();
        await using (db)
        {
            var command = new IngestRspK22MessageCommand(RspK22Order001WithNewSetting, null, PrescriptionConflictPolicy.Partial);

            IngestRspK22MessageResponse response = await handler.HandleAsync(command);

            response.OrderId.ShouldBe("ORD001");
            response.SettingsCount.ShouldBe(1);
            response.Success.ShouldBeTrue();

            PrescriptionEntity? merged = await db.Prescriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.OrderId == "ORD001");

            merged = merged.ShouldNotBeNull();
            merged.Settings.Count.ShouldBe(4);
            merged.Settings.Select(s => s.Code).ShouldContain("MDC_HDIALY_DIALYSATE_FLOW_RATE_SETTING");
            merged.Settings.First(s => s.Code.Contains("BLOOD_FLOW", StringComparison.OrdinalIgnoreCase)).ConstantValue.ShouldBe(300m);
            merged.Settings.First(s => s.Code.Contains("DIALYSATE", StringComparison.OrdinalIgnoreCase)).ConstantValue.ShouldBe(500m);
        }
    }

    private static async Task<(PrescriptionDbContext Db, IngestRspK22MessageCommandHandler Handler)> CreateDbAndHandlerAsync()
    {
        string dbName = "IngestConflict_" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<PrescriptionDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        await using (var setupDb = new PrescriptionDbContext(options))
        {
            _ = await setupDb.Database.EnsureCreatedAsync();

            var prescription = PrescriptionEntity.Create(
                "ORD001",
                new MedicalRecordNumber("MRN123"),
                "HD",
                "PROVIDER",
                "555-1111",
                TenantContext.DefaultTenantId);

            prescription.AddSetting(ProfileSetting.Constant("MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING", 300, null, "RSET"));
            prescription.AddSetting(ProfileSetting.Constant("MDC_HDIALY_UF_RATE_SETTING", 500, null, "RSET"));
            prescription.AddSetting(ProfileSetting.Constant("MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE", 2000, null, "RSET"));

            _ = setupDb.Prescriptions.Add(prescription);
            _ = await setupDb.SaveChangesAsync();
        }

        var db = new PrescriptionDbContext(options);
        var handler = CreateHandler(db);
        return (db, handler);
    }

    private static IngestRspK22MessageCommandHandler CreateHandler(PrescriptionDbContext db)
    {
        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new PrescriptionRepository(db, tenant);
        var parser = new RspK22Parser();
        var validator = new RspK22Validator();
        return new IngestRspK22MessageCommandHandler(parser, validator, repository, tenant);
    }
}
