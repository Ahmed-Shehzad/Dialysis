using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Dialysis.Patient.Application.Domain.ValueObjects;
using Dialysis.Patient.Application.Features.ProcessQbpQ22Query;
using Dialysis.Patient.Application.Features.RegisterPatient;
using Dialysis.Patient.Infrastructure.Hl7;
using Dialysis.Patient.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Shouldly;

namespace Dialysis.Patient.Tests;

/// <summary>
/// End-to-end integration tests: Register patient → Process QBP^Q22 → RSP^K22 with PID.
/// </summary>
public sealed class QbpQ22ToRspK22IntegrationTests
{
    private const string QbpQ22ByMrn = """
        MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||QBP^Q22^QBP_Q21|MSG001|P|2.6
        QPD|IHE PDQ Query^IHE PDQ Query^IHE|Q001|@PID.3^MRN123^^^^MR
        RCP|I||RD
        """;

    [Fact]
    public async Task QbpQ22_RegisteredPatient_ReturnsRspK22WithPidAsync()
    {
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
            new MedicalRecordNumber("MRN123"),
            new Person("John", "Doe"),
            DateOnly.FromDateTime(new DateTime(1990, 5, 15, 0, 0, 0, DateTimeKind.Unspecified)),
            Gender.Male));

        ProcessQbpQ22QueryResponse response = await processHandler.HandleAsync(new ProcessQbpQ22QueryCommand(QbpQ22ByMrn));

        response.MatchCount.ShouldBe(1);
        response.RspK22Message.ShouldContain("MSH|");
        response.RspK22Message.ShouldContain("MSA|AA|");
        response.RspK22Message.ShouldContain("RSP^K22");
        response.RspK22Message.ShouldContain("PID|");
        response.RspK22Message.ShouldContain("MRN123");
        response.RspK22Message.ShouldContain("Doe");
        response.RspK22Message.ShouldContain("John");
#pragma warning restore IDE0058
    }


    [Fact]
    public async Task QbpQ22_NoMatchingPatient_ReturnsRspK22WithNfAsync()
    {
        await using PatientDbContext db = CreateDbContext();
        _ = await db.Database.EnsureCreatedAsync();

        var tenant = new TenantContext { TenantId = TenantContext.DefaultTenantId };
        var repository = new PatientRepository(db, tenant);
        var processHandler = new ProcessQbpQ22QueryCommandHandler(
            new QbpQ22Parser(),
            new PatientRspK22Builder(),
            repository);

        ProcessQbpQ22QueryResponse response = await processHandler.HandleAsync(new ProcessQbpQ22QueryCommand(QbpQ22ByMrn));

#pragma warning disable IDE0058
        response.MatchCount.ShouldBe(0);
        response.RspK22Message.ShouldContain("NF");
#pragma warning restore IDE0058
    }

    private static PatientDbContext CreateDbContext()
    {
        DbContextOptions<PatientDbContext> options = new DbContextOptionsBuilder<PatientDbContext>()
            .UseInMemoryDatabase("QbpQ22_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new PatientDbContext(options);
    }
}
