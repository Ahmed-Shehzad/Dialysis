using Dialysis.Cds.Api;

using Hl7.Fhir.Model;

using Shouldly;

using Xunit;

namespace Dialysis.Cds.Tests;

/// <summary>
/// Unit tests for PrescriptionComplianceService.Evaluate.
/// </summary>
public sealed class PrescriptionComplianceServiceTests
{
    private readonly PrescriptionComplianceService _sut = new();

    [Fact]
    public void Evaluate_WhenPrescriptionIsNull_ReturnsNull()
    {
        var observations = new List<ObservationDto>
        {
            new("MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE", "300", "ml/min"),
        };
        DetectedIssue? result = _sut.Evaluate("session-1", "MRN001", observations, null);
        result.ShouldBeNull();
    }

    [Fact]
    public void Evaluate_WhenPatientMrnIsNullOrEmpty_ReturnsNull()
    {
        var prescription = new PrescriptionDto(300m, 500m, 2000m);
        var observations = new List<ObservationDto>
        {
            new("MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE", "250", "ml/min"),
        };
        DetectedIssue? result = _sut.Evaluate("session-1", null, observations, prescription);
        result.ShouldBeNull();

        result = _sut.Evaluate("session-1", "", observations, prescription);
        result.ShouldBeNull();
    }

    [Fact]
    public void Evaluate_WhenBloodFlowWithinTolerance_ReturnsNull()
    {
        var prescription = new PrescriptionDto(300m, 500m, 2000m);
        var observations = new List<ObservationDto>
        {
            new("MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE", "295", "ml/min"),
            new("MDC_HDIALY_UF_RATE", "490", "ml/h"),
            new("MDC_HDIALY_UF_ACTUAL_REMOVED_VOL", "1980", "ml"),
        };
        DetectedIssue? result = _sut.Evaluate("session-1", "MRN001", observations, prescription);
        result.ShouldBeNull();
    }

    [Fact]
    public void Evaluate_WhenBloodFlowExceedsTolerance_ReturnsDetectedIssue()
    {
        var prescription = new PrescriptionDto(300m, 500m, 2000m);
        var observations = new List<ObservationDto>
        {
            new("MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE", "200", "ml/min"),
        };
        DetectedIssue? result = _sut.Evaluate("session-1", "MRN001", observations, prescription);
        result.ShouldNotBeNull();
        result.Id.ShouldBe("cds-session-1");
        result.Code?.Coding?.FirstOrDefault()?.Code.ShouldBe("DEVV");
        result.Evidence.ShouldNotBeEmpty();
    }

    [Fact]
    public void Evaluate_WhenUfRateExceedsTolerance_ReturnsDetectedIssue()
    {
        var prescription = new PrescriptionDto(300m, 500m, 2000m);
        var observations = new List<ObservationDto>
        {
            new("MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE", "300", "ml/min"),
            new("MDC_HDIALY_UF_RATE", "600", "ml/h"),
        };
        DetectedIssue? result = _sut.Evaluate("session-1", "MRN001", observations, prescription);
        result.ShouldNotBeNull();
        result.Evidence.Count.ShouldBe(1);
    }

    [Fact]
    public void Evaluate_WhenUfActualVsTargetExceedsTolerance_ReturnsDetectedIssue()
    {
        var prescription = new PrescriptionDto(300m, 500m, 2000m);
        var observations = new List<ObservationDto>
        {
            new("MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE", "300", "ml/min"),
            new("MDC_HDIALY_UF_RATE", "500", "ml/h"),
            new("MDC_HDIALY_UF_ACTUAL_REMOVED_VOL", "1500", "ml"),
        };
        DetectedIssue? result = _sut.Evaluate("session-1", "MRN001", observations, prescription);
        result.ShouldNotBeNull();
        result.Evidence.Count.ShouldBe(1);
    }

    [Fact]
    public void Evaluate_WhenMultipleDeviations_ReturnsAllInEvidence()
    {
        var prescription = new PrescriptionDto(300m, 500m, 2000m);
        var observations = new List<ObservationDto>
        {
            new("MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE", "200", "ml/min"),
            new("MDC_HDIALY_UF_RATE", "600", "ml/h"),
            new("MDC_HDIALY_UF_ACTUAL_REMOVED_VOL", "1500", "ml"),
        };
        DetectedIssue? result = _sut.Evaluate("session-1", "MRN001", observations, prescription);
        result.ShouldNotBeNull();
        result.Evidence.Count.ShouldBe(3);
    }

    [Fact]
    public void Evaluate_WhenPrescribedIsZero_DoesNotFlagAsDeviation()
    {
        var prescription = new PrescriptionDto(0m, 0m, 0m);
        var observations = new List<ObservationDto>
        {
            new("MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE", "100", "ml/min"),
        };
        DetectedIssue? result = _sut.Evaluate("session-1", "MRN001", observations, prescription);
        result.ShouldBeNull();
    }
}
