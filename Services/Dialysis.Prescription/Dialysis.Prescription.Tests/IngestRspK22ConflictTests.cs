using BuildingBlocks.Tenancy;
using BuildingBlocks.Testcontainers;
using BuildingBlocks.ValueObjects;

using Dialysis.Prescription.Application.Domain;
using Dialysis.Prescription.Application.Domain.ValueObjects;
using Dialysis.Prescription.Application.Exceptions;
using Dialysis.Prescription.Application.Features.IngestRspK22Message;
using Dialysis.Prescription.Infrastructure.Hl7;
using Dialysis.Prescription.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using BuildingBlocks.TimeSync;

using Shouldly;

using PrescriptionEntity = Dialysis.Prescription.Application.Domain.Prescription;

namespace Dialysis.Prescription.Tests;

/// <summary>
/// Integration tests for prescription conflict handling: Callback (409 with callbackPhone), Partial merge.
/// Uses Testcontainers PostgreSQL for real database behavior.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class IngestRspK22ConflictTests
{
    private readonly PostgreSqlFixture _fixture;

    public IngestRspK22ConflictTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Callback_WhenConflict_ThrowsPrescriptionConflictExceptionWithCallbackPhoneAsync()
    {
        string mrn = PrescriptionTestData.Mrn();
        string orderId = PrescriptionTestData.OrderId();
        string phone1 = PrescriptionTestData.CallbackPhone();
        string rspK22First = PrescriptionTestData.RspK22ConflictMessage(new RspK22ConflictParams(mrn, orderId, phone1));

        (PrescriptionDbContext db, IngestRspK22MessageCommandHandler handler) = await CreateDbAndHandlerAsync(mrn, orderId, phone1);
        await using (db)
        {
            var command = new IngestRspK22MessageCommand(rspK22First, null, PrescriptionConflictPolicy.Callback);

            PrescriptionConflictException ex = await Should.ThrowAsync<PrescriptionConflictException>(
                () => handler.HandleAsync(command));

            ex.OrderId.ShouldBe(orderId);
            ex.CallbackPhone.ShouldBe(phone1);
        }
    }

    [Fact]
    public async Task Partial_WhenConflict_MergesNewSettingsOnlyAsync()
    {
        string mrn = PrescriptionTestData.Mrn();
        string orderId = PrescriptionTestData.OrderId();
        string phone1 = PrescriptionTestData.CallbackPhone();
        string phone2 = PrescriptionTestData.CallbackPhone();
        string rspK22WithNewSetting = PrescriptionTestData.RspK22ConflictMessage(new RspK22ConflictParams(
            mrn, orderId, phone2,
            new RspK22ObxOverrides("20230215130000", "MSG002", 350, 600, 2500,
                "OBX|4|NM|12348^MDC_HDIALY_DIALYSATE_FLOW_RATE_SETTING^MDC||500|ml/min||||||||||RSET")));

        (PrescriptionDbContext db, IngestRspK22MessageCommandHandler handler) = await CreateDbAndHandlerAsync(mrn, orderId, phone1);
        await using (db)
        {
            var command = new IngestRspK22MessageCommand(rspK22WithNewSetting, null, PrescriptionConflictPolicy.Partial);

            IngestRspK22MessageResponse response = await handler.HandleAsync(command);

            response.OrderId.ShouldBe(orderId);
            response.SettingsCount.ShouldBe(1);
            response.Success.ShouldBeTrue();

            PrescriptionEntity? merged = await db.Prescriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.OrderId == orderId);

            merged = merged.ShouldNotBeNull();
            merged.Settings.Count.ShouldBe(4);
            merged.Settings.Select(s => s.Code).ShouldContain("MDC_HDIALY_DIALYSATE_FLOW_RATE_SETTING");
            merged.Settings.First(s => s.Code.Contains("BLOOD_FLOW", StringComparison.OrdinalIgnoreCase)).ConstantValue.ShouldBe(300m);
            merged.Settings.First(s => s.Code.Contains("DIALYSATE", StringComparison.OrdinalIgnoreCase)).ConstantValue.ShouldBe(500m);
        }
    }

    private async Task<(PrescriptionDbContext Db, IngestRspK22MessageCommandHandler Handler)> CreateDbAndHandlerAsync(string mrn, string orderId, string callbackPhone)
    {
        DbContextOptions<PrescriptionDbContext> options = new DbContextOptionsBuilder<PrescriptionDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        await using (var setupDb = new PrescriptionDbContext(options))
        {
            _ = await setupDb.Database.EnsureCreatedAsync();
            _ = await setupDb.Prescriptions.ExecuteDeleteAsync();

            var prescription = PrescriptionEntity.Create(
                new OrderId(orderId),
                new MedicalRecordNumber(mrn),
                "HD",
                "PROVIDER",
                callbackPhone,
                TenantContext.DefaultTenantId);

            prescription.AddSetting(ProfileSetting.Constant("MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING", 300, null, "RSET"));
            prescription.AddSetting(ProfileSetting.Constant("MDC_HDIALY_UF_RATE_SETTING", 500, null, "RSET"));
            prescription.AddSetting(ProfileSetting.Constant("MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE", 2000, null, "RSET"));

            _ = setupDb.Prescriptions.Add(prescription);
            _ = await setupDb.SaveChangesAsync();
        }

        var db = new PrescriptionDbContext(options);
        IngestRspK22MessageCommandHandler handler = CreateHandler(db);
        return (db, handler);
    }

    private static IngestRspK22MessageCommandHandler CreateHandler(PrescriptionDbContext db)
    {
        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new PrescriptionRepository(db, tenant);
        var parser = new RspK22Parser();
        var validator = new RspK22Validator();
        return new IngestRspK22MessageCommandHandler(
            parser, validator, repository, tenant,
            Options.Create(new TimeSyncOptions { MaxAllowedDriftSeconds = 0 }),
            NullLogger<IngestRspK22MessageCommandHandler>.Instance);
    }
}
