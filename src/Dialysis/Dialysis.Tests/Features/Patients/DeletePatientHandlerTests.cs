using Dialysis.DeviceIngestion.Features.Patients.Delete;
using Dialysis.Domain.Entities;
using Dialysis.Persistence;
using Dialysis.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Dialysis.Tests.Features.Patients;

public sealed class DeletePatientHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_false_when_patient_not_found()
    {
        var tenantId = new TenantId("default");
        var logicalId = new PatientId("patient-999");
        var db = TestDbContextFactory.CreateInMemory();
        var repository = new PatientRepository(db);
        var sut = new DeletePatientHandler(db, repository, NullLogger<DeletePatientHandler>.Instance);

        var command = new DeletePatientCommand(tenantId, logicalId);

        var result = await sut.HandleAsync(command);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_deletes_and_returns_true_when_patient_exists()
    {
        var tenantId = new TenantId("default");
        var logicalId = new PatientId("patient-001");
        var patient = Patient.Create(tenantId, logicalId, "Smith", "John", null);
        var db = await TestDbContextFactory.CreateWithPatientAsync(patient);
        var repository = new PatientRepository(db);
        var sut = new DeletePatientHandler(db, repository, NullLogger<DeletePatientHandler>.Instance);

        var command = new DeletePatientCommand(tenantId, logicalId);

        var result = await sut.HandleAsync(command);

        result.ShouldBeTrue();
        var deleted = await db.Patients.FirstOrDefaultAsync(p =>
            p.TenantId == tenantId && p.LogicalId == logicalId);
        deleted.ShouldBeNull();
    }
}
