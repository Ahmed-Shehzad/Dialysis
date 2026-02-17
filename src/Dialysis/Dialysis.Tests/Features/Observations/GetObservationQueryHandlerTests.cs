using Dialysis.DeviceIngestion.Features.Observations.Get;
using Dialysis.Domain.Aggregates;
using Dialysis.SharedKernel.ValueObjects;
using Shouldly;
using Xunit;

namespace Dialysis.Tests.Features.Observations;

public sealed class GetObservationQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_null_when_observation_not_found()
    {
        var tenantId = new TenantId("default");
        var observationId = new ObservationId("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        var db = TestDbContextFactory.CreateInMemory();
        var sut = new GetObservationQueryHandler(db);

        var query = new GetObservationQuery(tenantId, observationId);

        var result = await sut.HandleAsync(query);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_returns_observation_when_found()
    {
        var tenantId = new TenantId("default");
        var patientId = new PatientId("patient-001");
        var observation = Observation.Create(
            tenantId,
            patientId,
            LoincCode.BloodPressure,
            "Blood pressure",
            UnitOfMeasure.MillimetersOfMercury,
            120m,
            ObservationEffective.UtcNow);
        var db = await TestDbContextFactory.CreateWithObservationAsync(observation);
        var sut = new GetObservationQueryHandler(db);

        var observationId = new ObservationId(observation.Id.ToString());
        var query = new GetObservationQuery(tenantId, observationId);

        var result = await sut.HandleAsync(query);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(observation.Id);
    }
}
