using Dialysis.DeviceIngestion.Features.Patients.List;
using Dialysis.Domain.Entities;
using Dialysis.SharedKernel.ValueObjects;
using Shouldly;
using Xunit;

namespace Dialysis.Tests.Features.Patients;

public sealed class ListPatientsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_delegates_to_repository_with_query_params()
    {
        var tenantId = new TenantId("default");
        var patient = Patient.Create(tenantId, new PatientId("p1"), "Smith", "John", null);
        var db = await TestDbContextFactory.CreateWithPatientAsync(patient);
        var sut = new ListPatientsQueryHandler(db);

        var query = new ListPatientsQuery(tenantId, Family: "Smith", Given: "John", Count: 20, Offset: 0);
        var result = await sut.HandleAsync(query);

        result.ShouldHaveSingleItem();
        result[0].LogicalId.Value.ShouldBe("p1");
        result[0].FamilyName.ShouldBe("Smith");
    }

    [Fact]
    public async Task HandleAsync_returns_empty_list_when_no_matches()
    {
        var tenantId = new TenantId("default");
        var db = TestDbContextFactory.CreateInMemory();
        var sut = new ListPatientsQueryHandler(db);

        var query = new ListPatientsQuery(tenantId);
        var result = await sut.HandleAsync(query);

        result.ShouldBeEmpty();
    }
}
