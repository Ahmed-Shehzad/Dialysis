using Dialysis.Hl7ToFhir;

using Hl7.Fhir.Model;

using Shouldly;

using Xunit;

namespace Dialysis.Hl7ToFhir.Tests;

/// <summary>
/// Unit tests for ProcedureMapper (FHIR R4 Procedure from treatment session).
/// </summary>
public sealed class ProcedureMapperTests
{
    [Fact]
    public void ToFhirProcedure_WhenPatientIdIsNull_SetsSubjectToUnknownPlaceholder()
    {
        Procedure proc = ProcedureMapper.ToFhirProcedure(
            "sess-1", null, "device-1", "Completed", null, null);

        proc.Subject.ShouldNotBeNull().Reference.ShouldBe("Patient/unknown");
        proc.Code.ShouldNotBeNull();
        proc.Status.ShouldBe(EventStatus.Completed);
    }

    [Fact]
    public void ToFhirProcedure_WhenPatientIdProvided_SetsSubjectReference()
    {
        Procedure proc = ProcedureMapper.ToFhirProcedure(
            "sess-1", "MRN001", "device-1", "Active", null, null);

        proc.Subject.ShouldNotBeNull().Reference.ShouldBe("Patient/MRN001");
    }

    [Fact]
    public void ToFhirProcedure_WhenStatusActive_MapsToInProgress()
    {
        Procedure proc = ProcedureMapper.ToFhirProcedure(
            "sess-1", "MRN001", "device-1", "Active", null, null);

        proc.Status.ShouldBe(EventStatus.InProgress);
    }

    [Fact]
    public void ToFhirProcedure_WhenStatusCompleted_MapsToCompleted()
    {
        Procedure proc = ProcedureMapper.ToFhirProcedure(
            "sess-1", "MRN001", "device-1", "Completed", null, null);

        proc.Status.ShouldBe(EventStatus.Completed);
    }

    [Fact]
    public void ToFhirProcedure_AlwaysSetsHemodialysisCode()
    {
        Procedure proc = ProcedureMapper.ToFhirProcedure(
            "sess-1", null, null, "Completed", null, null);

        proc.Code.ShouldNotBeNull();
        proc.Code.Coding.ShouldContain(c => c.Code == "1088001" && c.System == "http://snomed.info/sct");
        proc.Code.Text.ShouldBe("Hemodialysis");
    }

    [Fact]
    public void ToFhirProcedure_WhenDeviceIdProvided_AddsPerformer()
    {
        Procedure proc = ProcedureMapper.ToFhirProcedure(
            "sess-1", "MRN001", "device-eui64-1", "Active", null, null);

        proc.Performer.ShouldNotBeEmpty();
        proc.Performer![0].Actor.ShouldNotBeNull().Reference.ShouldBe("Device/device-eui64-1");
    }

    [Fact]
    public void ToFhirProcedure_WhenStartedAndEndedAtProvided_SetsPerformedPeriod()
    {
        var started = new DateTimeOffset(2025, 2, 20, 8, 0, 0, TimeSpan.Zero);
        var ended = new DateTimeOffset(2025, 2, 20, 12, 0, 0, TimeSpan.Zero);

        Procedure proc = ProcedureMapper.ToFhirProcedure(
            "sess-1", "MRN001", "device-1", "Completed", started, ended);

        proc.Performed.ShouldBeOfType<Period>();
        var period = (Period)proc.Performed!;
        period.Start.ShouldNotBeNull();
        period.End.ShouldNotBeNull();
    }

    [Fact]
    public void ToFhirProcedure_AlwaysAddsSessionIdentifier()
    {
        Procedure proc = ProcedureMapper.ToFhirProcedure(
            "sess-abc-123", null, null, "Completed", null, null);

        proc.Identifier.ShouldNotBeEmpty();
        proc.Identifier.ShouldContain(i => i.System == "urn:dialysis:session" && i.Value == "sess-abc-123");
    }
}
