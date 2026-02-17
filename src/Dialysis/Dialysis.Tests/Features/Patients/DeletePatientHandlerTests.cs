using Dialysis.DeviceIngestion.Features.Patients.Delete;
using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Dialysis.Tests.Features.Patients;

public sealed class DeletePatientHandlerTests
{
    private readonly IPatientRepository _repository;
    private readonly DeletePatientHandler _sut;

    public DeletePatientHandlerTests()
    {
        _repository = Substitute.For<IPatientRepository>();
        _sut = new DeletePatientHandler(_repository, Substitute.For<ILogger<DeletePatientHandler>>());
    }

    [Fact]
    public async Task HandleAsync_returns_false_when_patient_not_found()
    {
        var tenantId = new TenantId("default");
        var logicalId = new PatientId("patient-999");
        var command = new DeletePatientCommand(tenantId, logicalId);

        _repository.GetByIdAsync(tenantId, logicalId, Arg.Any<CancellationToken>()).Returns((Patient?)null);

        var result = await _sut.HandleAsync(command);

        result.ShouldBeFalse();
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_deletes_and_returns_true_when_patient_exists()
    {
        var tenantId = new TenantId("default");
        var logicalId = new PatientId("patient-001");
        var patient = Patient.Create(tenantId, logicalId, "Smith", "John", null);
        var command = new DeletePatientCommand(tenantId, logicalId);

        _repository.GetByIdAsync(tenantId, logicalId, Arg.Any<CancellationToken>()).Returns(patient);

        var result = await _sut.HandleAsync(command);

        result.ShouldBeTrue();
        await _repository.Received(1).DeleteAsync(patient, Arg.Any<CancellationToken>());
    }
}
