using BuildingBlocks.Tenancy;
using BuildingBlocks.Testcontainers;
using BuildingBlocks.ValueObjects;

using Dialysis.Patient.Application.Features.ProcessQbpQ22Query;
using Dialysis.Patient.Application.Features.RegisterPatient;
using Dialysis.Patient.Infrastructure.Hl7;
using Dialysis.Patient.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace Dialysis.Patient.Tests;

/// <summary>
/// End-to-end integration tests: Register patient → Process QBP^Q22 → RSP^K22 with PID.
/// Uses Testcontainers PostgreSQL for real database behavior.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class QbpQ22ToRspK22IntegrationTests
{
    private readonly PostgreSqlFixture _fixture;

    public QbpQ22ToRspK22IntegrationTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task QbpQ22_RegisteredPatient_ReturnsRspK22WithPidAsync()
    {
        string mrn = PatientTestData.Mrn();
        var person = PatientTestData.Person();
        string qbpQ22 = PatientTestData.QbpQ22ByMrn(mrn);

        await using PatientDbContext db = CreateDbContext();
#pragma warning disable IDE0058
        _ = await db.Database.EnsureCreatedAsync();

        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new PatientRepository(db, tenant);
        var registerHandler = new RegisterPatientCommandHandler(repository, tenant);
        var processHandler = new ProcessQbpQ22QueryCommandHandler(
            new QbpQ22Parser(),
            new PatientRspK22Builder(),
            repository);

#pragma warning disable IDE0058
        _ = await registerHandler.HandleAsync(new RegisterPatientCommand(
            new MedicalRecordNumber(mrn),
            person,
            PatientTestData.DateOfBirth(),
            PatientTestData.Gender()));

        ProcessQbpQ22QueryResponse response = await processHandler.HandleAsync(new ProcessQbpQ22QueryCommand(qbpQ22));

        response.MatchCount.ShouldBe(1);
        response.RspK22Message.ShouldContain("MSH|");
        response.RspK22Message.ShouldContain("MSA|AA|");
        response.RspK22Message.ShouldContain("RSP^K22");
        response.RspK22Message.ShouldContain("PID|");
        response.RspK22Message.ShouldContain(mrn);
        response.RspK22Message.ShouldContain(person.LastName);
        response.RspK22Message.ShouldContain(person.FirstName);
#pragma warning restore IDE0058
    }


    [Fact]
    public async Task QbpQ22_NoMatchingPatient_ReturnsRspK22WithNfAsync()
    {
        await using PatientDbContext db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Patients.ExecuteDeleteAsync();

        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new PatientRepository(db, tenant);
        var processHandler = new ProcessQbpQ22QueryCommandHandler(
            new QbpQ22Parser(),
            new PatientRspK22Builder(),
            repository);

        string qbpQ22 = PatientTestData.QbpQ22ByMrn(PatientTestData.Mrn());
        ProcessQbpQ22QueryResponse response = await processHandler.HandleAsync(new ProcessQbpQ22QueryCommand(qbpQ22));

#pragma warning disable IDE0058
        response.MatchCount.ShouldBe(0);
        response.RspK22Message.ShouldContain("NF");
#pragma warning restore IDE0058
    }

    private PatientDbContext CreateDbContext()
    {
        DbContextOptions<PatientDbContext> options = new DbContextOptionsBuilder<PatientDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new PatientDbContext(options);
    }
}
