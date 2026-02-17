using Dialysis.DeviceIngestion.Features.Patients.Update;
using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Dialysis.Tests.Features.Patients;

public sealed class UpdatePatientHandlerTests
{
    private readonly IPatientRepository _repository;
    private readonly UpdatePatientHandler _sut;

    public UpdatePatientHandlerTests()
    {
        _repository = Substitute.For<IPatientRepository>();
        _sut = new UpdatePatientHandler(_repository, Substitute.For<ILogger<UpdatePatientHandler>>());
    }

    [Fact]
    public async Task HandleAsync_returns_null_when_patient_not_found()
    {
        var tenantId = new TenantId("default");
        var logicalId = new PatientId("patient-999");
        var command = new UpdatePatientCommand(tenantId, logicalId, "Jones", "Jane", null);

        _repository.GetByIdAsync(tenantId, logicalId, Arg.Any<CancellationToken>()).Returns((Patient?)null);

        var result = await _sut.HandleAsync(command);

        result.ShouldBeNull();
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_updates_and_returns_result_when_patient_exists()
    {
        var tenantId = new TenantId("default");
        var logicalId = new PatientId("patient-001");
        var patient = Patient.Create(tenantId, logicalId, "Smith", "John", null);
        var command = new UpdatePatientCommand(tenantId, logicalId, "Jones", "Jane", new DateTime(1985, 3, 15));

        _repository.GetByIdAsync(tenantId, logicalId, Arg.Any<CancellationToken>()).Returns(patient);

        var result = await _sut.HandleAsync(command);

        result.ShouldNotBeNull();
        result!.LogicalId.Value.ShouldBe("patient-001");
        result.FamilyName.ShouldBe("Jones");
        result.GivenNames.ShouldBe("Jane");
        result.BirthDate.ShouldBe(new DateTime(1985, 3, 15));
        await _repository.Received(1).UpdateAsync(patient, Arg.Any<CancellationToken>());
    }
}
