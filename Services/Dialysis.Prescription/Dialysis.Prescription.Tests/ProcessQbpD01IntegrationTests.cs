using BuildingBlocks.Tenancy;
using BuildingBlocks.Testcontainers;
using BuildingBlocks.ValueObjects;

using Dialysis.Prescription.Application.Domain;
using Dialysis.Prescription.Application.Features.IngestRspK22Message;
using Dialysis.Prescription.Application.Features.ProcessQbpD01Query;
using Dialysis.Prescription.Infrastructure.Hl7;
using Dialysis.Prescription.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace Dialysis.Prescription.Tests;

/// <summary>
/// Integration test: QBP^D01 → ProcessQbpD01QueryCommandHandler → RSP^K22 round-trip.
/// Uses Testcontainers PostgreSQL for real database behavior.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class ProcessQbpD01IntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public ProcessQbpD01IntegrationTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task RoundTrip_PrescriptionExists_ReturnsRspK22WithPrescriptionAsync()
    {
        string mrn = PrescriptionTestData.Mrn();
        string orderId = PrescriptionTestData.OrderId();
        string provider = PrescriptionTestData.OrderingProvider();
        string phone = PrescriptionTestData.CallbackPhone();
        string qbpD01 = PrescriptionTestData.QbpD01ByMrn(mrn);

        await using var db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Prescriptions.ExecuteDeleteAsync();

        var prescription = Dialysis.Prescription.Application.Domain.Prescription.Create(
            orderId,
            new MedicalRecordNumber(mrn),
            "HD",
            provider,
            phone);

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
        var command = new ProcessQbpD01QueryCommand(qbpD01);

        ProcessQbpD01QueryResponse response = await handler.HandleAsync(command);

        response.Mrn.ShouldBe(mrn);
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
        string mrn = PrescriptionTestData.Mrn();
        string qbpD01 = PrescriptionTestData.QbpD01ByMrn(mrn);

        await using var db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Prescriptions.ExecuteDeleteAsync();

        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new PrescriptionRepository(db, tenant);
        var parser = new QbpD01Parser();
        var builder = new RspK22Builder();

        var handler = new ProcessQbpD01QueryCommandHandler(parser, builder, repository);
        var command = new ProcessQbpD01QueryCommand(qbpD01);

        ProcessQbpD01QueryResponse response = await handler.HandleAsync(command);

        response.Mrn.ShouldBe(mrn);
        response.RspK22Message.ShouldContain("MSA|AA|");
        response.RspK22Message.ShouldContain("QAK|");
        response.RspK22Message.ShouldContain("NF");
        response.RspK22Message.ShouldNotContain("ORC|");
        response.RspK22Message.ShouldNotContain("OBX|");
    }

    /// <summary>
    /// Full E2E: Ingest RSP^K22 (EMR→PDMS) → QBP^D01 query (machine→PDMS) → RSP^K22 response.
    /// Verifies the round-trip: prescription stored via ingest is correctly returned on query.
    /// </summary>
    [Fact]
    public async Task RspK22IngestThenQbpD01Query_ReturnsMatchingRspK22Async()
    {
        string mrn = PrescriptionTestData.Mrn();
        string orderId = PrescriptionTestData.OrderId();
        string provider = PrescriptionTestData.OrderingProvider();
        string phone = PrescriptionTestData.CallbackPhone();
        string rspK22 = PrescriptionTestData.MinimalRspK22(mrn, orderId, provider, phone);
        string qbpD01 = PrescriptionTestData.QbpD01ByMrn(mrn);

        await using var db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Prescriptions.ExecuteDeleteAsync();

        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new PrescriptionRepository(db, tenant);
        var rspParser = new RspK22Parser();
        var rspValidator = new RspK22Validator();
        var qbpParser = new QbpD01Parser();
        var rspBuilder = new RspK22Builder();

        var ingestHandler = new IngestRspK22MessageCommandHandler(rspParser, rspValidator, repository, tenant);
        var queryHandler = new ProcessQbpD01QueryCommandHandler(qbpParser, rspBuilder, repository);

        IngestRspK22MessageResponse ingestResponse = await ingestHandler.HandleAsync(
            new IngestRspK22MessageCommand(rspK22, null, PrescriptionConflictPolicy.Replace));
        ingestResponse.Success.ShouldBeTrue();
        ingestResponse.SettingsCount.ShouldBe(3);

        ProcessQbpD01QueryResponse queryResponse = await queryHandler.HandleAsync(new ProcessQbpD01QueryCommand(qbpD01));

        queryResponse.Mrn.ShouldBe(mrn);
        queryResponse.RspK22Message.ShouldContain("MSH|");
        queryResponse.RspK22Message.ShouldContain("MSA|AA|");
        queryResponse.RspK22Message.ShouldContain("RSP^K22");
        queryResponse.RspK22Message.ShouldContain("ORC|");
        queryResponse.RspK22Message.ShouldContain("OBX|");
        queryResponse.RspK22Message.ShouldContain("300");
        queryResponse.RspK22Message.ShouldContain("500");
        queryResponse.RspK22Message.ShouldContain("2000");
    }

    private PrescriptionDbContext CreateDbContext()
    {
        DbContextOptions<PrescriptionDbContext> options = new DbContextOptionsBuilder<PrescriptionDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new PrescriptionDbContext(options);
    }
}
