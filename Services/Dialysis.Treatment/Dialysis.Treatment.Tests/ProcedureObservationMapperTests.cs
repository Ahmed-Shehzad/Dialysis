using Dialysis.Hl7ToFhir;

using Shouldly;

#pragma warning disable IDE0058 // Expression value is never used - assertions

namespace Dialysis.Treatment.Tests;

/// <summary>
/// Unit tests for ProcedureMapper and ObservationMapper FHIR R4 cardinality compliance.
/// Ensures required elements (Procedure.subject, Observation.code) are always present.
/// </summary>
public sealed class ProcedureObservationMapperTests
{
    [Fact]
    public void ProcedureMapper_WhenPatientIdIsNull_SetsSubjectToUnknownPlaceholder()
    {
        var proc = ProcedureMapper.ToFhirProcedure(
            "sess-1", null, "device-1", "Completed", null, null);

        proc.Subject.ShouldNotBeNull().Reference.ShouldBe("Patient/unknown");
        proc.Code.ShouldNotBeNull();
        proc.Status.ShouldBe(Hl7.Fhir.Model.EventStatus.Completed);
    }

    [Fact]
    public void ProcedureMapper_WhenPatientIdProvided_SetsSubjectReference()
    {
        var proc = ProcedureMapper.ToFhirProcedure(
            "sess-1", "MRN001", "device-1", "Active", null, null);

        proc.Subject.ShouldNotBeNull().Reference.ShouldBe("Patient/MRN001");
    }

    [Fact]
    public void ObservationMapper_WhenObservationCodeEmpty_UsesMdcUnknownPlaceholder()
    {
        var input = new ObservationMappingInput(
            "", "120", "mmHg", null, null, null, null, "device-1", "MRN001", null);

        var obs = ObservationMapper.ToFhirObservation(input);

        obs.Code.ShouldNotBeNull();
        obs.Code.Coding.ShouldNotBeEmpty();
        obs.Code.Coding[0].Code.ShouldBe("MDC_UNKNOWN");
        obs.Code.Coding[0].System.ShouldBe("urn:iso:std:iso:11073:10101");
    }

    [Fact]
    public void ObservationMapper_WhenObservationCodeProvided_UsesActualCode()
    {
        var input = new ObservationMappingInput(
            "150456", "300", "ml/min", null, null, null, null, "device-1", "MRN001", null);

        var obs = ObservationMapper.ToFhirObservation(input);

        obs.Code.ShouldNotBeNull();
        obs.Code.Coding[0].Code.ShouldBe("150456");
    }
}
