using Dialysis.DeviceIngestion.Features.Patients.Update;
using Dialysis.Domain.Entities;
using Dialysis.Persistence;
using Dialysis.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Dialysis.Tests.Features.Patients;

public sealed class UpdatePatientHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_null_when_patient_not_found()
    {
        var tenantId = new TenantId("default");
        var logicalId = new PatientId("patient-999");
        var db = TestDbContextFactory.CreateInMemory();
        var repository = new PatientRepository(db);
        var sut = new UpdatePatientHandler(db, repository, NullLogger<UpdatePatientHandler>.Instance);

        var command = new UpdatePatientCommand(tenantId, logicalId, "Jones", "Jane", null);

        var result = await sut.HandleAsync(command);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_updates_and_returns_result_when_patient_exists()
    {
        var tenantId = new TenantId("default");
        var logicalId = new PatientId("patient-001");
        var patient = Patient.Create(tenantId, logicalId, "Smith", "John", null);
        var db = await TestDbContextFactory.CreateWithPatientAsync(patient);
        var repository = new PatientRepository(db);
        var sut = new UpdatePatientHandler(db, repository, NullLogger<UpdatePatientHandler>.Instance);

        var command = new UpdatePatientCommand(tenantId, logicalId, "Jones", "Jane", new DateTime(1985, 3, 15));

        var result = await sut.HandleAsync(command);

        result.ShouldNotBeNull();
        result!.LogicalId.Value.ShouldBe("patient-001");
        result.FamilyName.ShouldBe("Jones");
        result.GivenNames.ShouldBe("Jane");
        result.BirthDate.ShouldBe(new DateTime(1985, 3, 15));
    }
}
