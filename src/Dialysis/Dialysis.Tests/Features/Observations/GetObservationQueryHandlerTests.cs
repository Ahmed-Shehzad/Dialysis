using Dialysis.DeviceIngestion.Features.Observations.Get;
using Dialysis.Domain.Aggregates;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.ValueObjects;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Dialysis.Tests.Features.Observations;

public sealed class GetObservationQueryHandlerTests
{
    private readonly IObservationRepository _repository;
    private readonly GetObservationQueryHandler _sut;

    public GetObservationQueryHandlerTests()
    {
        _repository = Substitute.For<IObservationRepository>();
        _sut = new GetObservationQueryHandler(_repository);
    }

    [Fact]
    public async Task HandleAsync_returns_null_when_observation_not_found()
    {
        var tenantId = new TenantId("default");
        var observationId = new ObservationId("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        var query = new GetObservationQuery(tenantId, observationId);

        _repository.GetByIdAsync(tenantId, observationId, Arg.Any<CancellationToken>()).Returns((Observation?)null);

        var result = await _sut.HandleAsync(query);

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
        var observationId = new ObservationId(observation.Id.ToString());
        var query = new GetObservationQuery(tenantId, observationId);

        _repository.GetByIdAsync(tenantId, observationId, Arg.Any<CancellationToken>()).Returns(observation);

        var result = await _sut.HandleAsync(query);

        result.ShouldBe(observation);
        await _repository.Received(1).GetByIdAsync(tenantId, observationId, Arg.Any<CancellationToken>());
    }
}
