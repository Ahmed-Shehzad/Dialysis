using Hl7.Fhir.Model;

using Shouldly;

using Xunit;

namespace Dialysis.Hl7ToFhir.Tests;

/// <summary>
/// Unit tests for ObservationMapper (FHIR R4, QI-Core device observation pattern).
/// </summary>
public sealed class ObservationMapperTests
{
    [Fact]
    public void ToFhirObservation_WhenDeviceIdProvided_SetsDeviceAndFocus()
    {
        var input = new ObservationMappingInput(
            "150456", "300", "ml/min", null, null, null, null, "device-eui64-1", "MRN001", null);

        Observation obs = ObservationMapper.ToFhirObservation(input);

        obs.Device.ShouldNotBeNull().Reference.ShouldBe("Device/device-eui64-1");
        obs.Focus.ShouldNotBeNull().ShouldHaveSingleItem().Reference.ShouldBe("Device/device-eui64-1");
    }

    [Fact]
    public void ToFhirObservation_WhenProvenanceProvided_AddsNote()
    {
        var input = new ObservationMappingInput(
            "150456", "120", "mmHg", null, null, "RSET", null, "device-1", null, null);

        Observation obs = ObservationMapper.ToFhirObservation(input);

        obs.Note.ShouldNotBeEmpty();
        (obs.Note![0].Text ?? "").ShouldContain("RSET");
    }

    [Fact]
    public void ToFhirObservation_WhenNumericValueAndUnit_SetsValueQuantityWithUcum()
    {
        var input = new ObservationMappingInput(
            "150456", "300", "ml/min", null, null, null, null, "device-1", null, null);

        Observation obs = ObservationMapper.ToFhirObservation(input);

        obs.Value.ShouldBeOfType<Hl7.Fhir.Model.Quantity>();
        var qty = (Hl7.Fhir.Model.Quantity)obs.Value!;
        qty.Value.ShouldBe(300);
        qty.Unit.ShouldBe("ml/min");
        qty.System.ShouldBe("http://unitsofmeasure.org");
    }

    [Fact]
    public void ToFhirObservation_WhenReferenceRangeProvided_SetsReferenceRange()
    {
        var input = new ObservationMappingInput(
            "150456", "120", "mmHg", null, "70-180", null, null, "device-1", null, null);

        Observation obs = ObservationMapper.ToFhirObservation(input);

        obs.ReferenceRange.ShouldNotBeEmpty();
        obs.ReferenceRange![0].Text.ShouldBe("70-180");
    }

    [Fact]
    public void ToFhirObservation_AlwaysSetsCategoryDevice()
    {
        var input = new ObservationMappingInput(
            "150456", "50", "ml/h", null, null, null, null, null, null, null);

        Observation obs = ObservationMapper.ToFhirObservation(input);

        obs.Category.ShouldNotBeEmpty();
        obs.Category![0].Coding.ShouldContain(c => c.Code == "device");
    }
}
