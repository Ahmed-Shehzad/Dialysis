using Dialysis.DeviceIngestion.Features.Patients.List;
using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.ValueObjects;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Dialysis.Tests.Features.Patients;

public sealed class ListPatientsQueryHandlerTests
{
    private readonly IPatientRepository _repository;
    private readonly ListPatientsQueryHandler _sut;

    public ListPatientsQueryHandlerTests()
    {
        _repository = Substitute.For<IPatientRepository>();
        _sut = new ListPatientsQueryHandler(_repository);
    }

    [Fact]
    public async Task HandleAsync_delegates_to_repository_with_query_params()
    {
        var tenantId = new TenantId("default");
        var query = new ListPatientsQuery(tenantId, Family: "Smith", Given: "John", Count: 20, Offset: 10);
        var patients = new List<Patient>
        {
            Patient.Create(tenantId, new PatientId("p1"), "Smith", "John", null)
        };
        _repository.ListAsync(tenantId, "Smith", "John", 20, 10, Arg.Any<CancellationToken>())
            .Returns(patients);

        var result = await _sut.HandleAsync(query);

        result.ShouldHaveSingleItem();
        result[0].LogicalId.Value.ShouldBe("p1");
        result[0].FamilyName.ShouldBe("Smith");
    }

    [Fact]
    public async Task HandleAsync_returns_empty_list_when_no_matches()
    {
        var tenantId = new TenantId("default");
        var query = new ListPatientsQuery(tenantId);
        _repository.ListAsync(tenantId, null, null, null, 0, Arg.Any<CancellationToken>())
            .Returns(new List<Patient>());

        var result = await _sut.HandleAsync(query);

        result.ShouldBeEmpty();
    }
}
