using Dialysis.DeviceIngestion.Features.Patients.Create;
using Dialysis.Domain.Entities;
using Dialysis.Persistence;
using Dialysis.SharedKernel.Exceptions;
using Dialysis.SharedKernel.ValueObjects;
using Intercessor.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Dialysis.Tests.Features.Patients;

public sealed class CreatePatientHandlerTests
{
    [Fact]
    public async Task HandleAsync_throws_PatientAlreadyExistsException_when_patient_exists()
    {
        var tenantId = new TenantId("default");
        var logicalId = new PatientId("patient-001");
        var existingPatient = Patient.Create(tenantId, logicalId, "Smith", "John", null);
        var db = await TestDbContextFactory.CreateWithPatientAsync(existingPatient);
        var repository = new PatientRepository(db);
        var publisher = Substitute.For<IPublisher>();
        var sut = new CreatePatientHandler(db, repository, publisher, NullLogger<CreatePatientHandler>.Instance);

        var command = new CreatePatientCommand(tenantId, logicalId, "Jones", "Jane", null);

        var ex = await Should.ThrowAsync<PatientAlreadyExistsException>(
            () => sut.HandleAsync(command));

        ex.Message.ShouldContain("patient-001");
        ex.Message.ShouldContain("default");
    }

    [Fact]
    public async Task HandleAsync_creates_patient_and_returns_result_when_not_exists()
    {
        var tenantId = new TenantId("default");
        var logicalId = new PatientId("patient-001");
        var db = TestDbContextFactory.CreateInMemory();
        var repository = new PatientRepository(db);
        var publisher = Substitute.For<IPublisher>();
        var sut = new CreatePatientHandler(db, repository, publisher, NullLogger<CreatePatientHandler>.Instance);

        var command = new CreatePatientCommand(tenantId, logicalId, "Smith", "John", new DateTime(1990, 1, 15));

        var result = await sut.HandleAsync(command);

        result.LogicalId.Value.ShouldBe("patient-001");
        var saved = await db.Patients.FirstOrDefaultAsync(p =>
            p.TenantId == tenantId && p.LogicalId == logicalId);
        saved.ShouldNotBeNull();
        saved!.FamilyName.ShouldBe("Smith");
        saved.GivenNames.ShouldBe("John");
        saved.BirthDate.ShouldBe(new DateTime(1990, 1, 15));
    }
}
