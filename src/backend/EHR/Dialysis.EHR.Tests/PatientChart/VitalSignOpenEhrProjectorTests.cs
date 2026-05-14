using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Projections;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.PatientChart;

public sealed class VitalSignOpenEhrProjectorTests
{
    private static readonly VitalSignOpenEhrProjector _sut = new();

    [Fact]
    public void Bodyweight_Loinc_Projects_To_Body_Weight_Archetype()
    {
        var reading = VitalSignReading.Record(
            id: Guid.CreateVersion7(),
            patientId: Guid.NewGuid(),
            observationType: new Coding(EhrCodeSystems.Loinc, EhrLoincCodes.BodyWeight, "Body weight"),
            value: 72.5m,
            unitCode: "kg",
            observedAtUtc: DateTime.UtcNow);

        var projection = _sut.Project(reading);

        projection.ShouldNotBeNull();
        projection.ArchetypeId.ShouldBe(OpenEhrArchetypes.BodyWeight);
        projection.CompositionJson.ShouldContain("\"magnitude\":72.5");
        projection.CompositionJson.ShouldContain("\"unit\":\"kg\"");
    }

    [Fact]
    public void Unknown_Loinc_Returns_Null()
    {
        var reading = VitalSignReading.Record(
            id: Guid.CreateVersion7(),
            patientId: Guid.NewGuid(),
            observationType: new Coding(EhrCodeSystems.Loinc, "00000-0", "Made-up code"),
            value: 1m,
            unitCode: "1",
            observedAtUtc: DateTime.UtcNow);

        _sut.Project(reading).ShouldBeNull();
    }
}
