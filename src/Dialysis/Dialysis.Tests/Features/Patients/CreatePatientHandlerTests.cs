using Dialysis.DeviceIngestion.Features.Patients.Create;
using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Exceptions;
using Dialysis.SharedKernel.ValueObjects;
using Intercessor.Abstractions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Dialysis.Tests.Features.Patients;

public sealed class CreatePatientHandlerTests
{
    private readonly IPatientRepository _repository;
    private readonly CreatePatientHandler _sut;

    public CreatePatientHandlerTests()
    {
        _repository = Substitute.For<IPatientRepository>();
        _sut = new CreatePatientHandler(_repository, Substitute.For<ILogger<CreatePatientHandler>>());
    }

    [Fact]
    public async Task HandleAsync_throws_PatientAlreadyExistsException_when_patient_exists()
    {
        var tenantId = new TenantId("default");
        var logicalId = new PatientId("patient-001");
        var command = new CreatePatientCommand(tenantId, logicalId, "Smith", "John", null);

        _repository.ExistsAsync(tenantId, logicalId, Arg.Any<CancellationToken>()).Returns(true);

        var ex = await Should.ThrowAsync<PatientAlreadyExistsException>(
            () => _sut.HandleAsync(command));

        ex.Message.ShouldContain("patient-001");
        ex.Message.ShouldContain("default");
        await _repository.DidNotReceive().AddAsync(Arg.Any<Patient>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_creates_patient_and_returns_result_when_not_exists()
    {
        var tenantId = new TenantId("default");
        var logicalId = new PatientId("patient-001");
        var command = new CreatePatientCommand(tenantId, logicalId, "Smith", "John", new DateTime(1990, 1, 15));

        _repository.ExistsAsync(tenantId, logicalId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.HandleAsync(command);

        result.LogicalId.Value.ShouldBe("patient-001");
        await _repository.Received(1).AddAsync(Arg.Is<Patient>(p =>
            p.TenantId == tenantId &&
            p.LogicalId == logicalId &&
            p.FamilyName == "Smith" &&
            p.GivenNames == "John" &&
            p.BirthDate == new DateTime(1990, 1, 15)),
            Arg.Any<CancellationToken>());
    }
}
