using Dialysis.CQRS;
using Dialysis.EHR.Persistence;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Features.RegisterPatient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests;

[Collection(nameof(EhrFixtureCollection))]
public sealed class RegistrationPatientFlowTests(EhrApiWebApplicationFactory factory)
{
    [Fact]
    public async Task Registerpatient_Persists_To_Postgres_And_Raises_Integration_Event_Async()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var patientId = await gateway.SendCommandAsync<RegisterPatientCommand, Guid>(
            new RegisterPatientCommand(
                MedicalRecordNumber: $"MRN-{Guid.NewGuid():N}",
                FamilyName: "Doe",
                GivenName: "Jane",
                MiddleName: null,
                DateOfBirth: new DateOnly(1985, 4, 12),
                SexAtBirthCode: "F",
                PreferredLanguageCode: "en"));

        patientId.ShouldNotBe(Guid.Empty);

        var db = scope.ServiceProvider.GetRequiredService<EhrDbContext>();
        var stored = await db.Patients.AsNoTracking().FirstAsync(p => p.Id == patientId);
        stored.Status.ShouldBe(PatientStatus.Active);
        stored.Name.FamilyName.ShouldBe("Doe");
        stored.Name.GivenName.ShouldBe("Jane");

        var outboxRowCount = await db.OutboxMessages.AsNoTracking().CountAsync();
        outboxRowCount.ShouldBeGreaterThanOrEqualTo(1);
    }
}

[CollectionDefinition(nameof(EhrFixtureCollection))]
public sealed class EhrFixtureCollection : ICollectionFixture<EhrApiWebApplicationFactory>;
