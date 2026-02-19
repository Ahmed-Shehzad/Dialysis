using Dialysis.Cds.Api;

using Hl7.Fhir.Model;

using Shouldly;

using Xunit;

namespace Dialysis.Cds.Tests;

public sealed class HypotensionRiskServiceTests
{
    private readonly HypotensionRiskService _sut = new();

    [Fact]
    public void Evaluate_WhenNoBpObservations_ReturnsNull()
    {
        var observations = new List<ObservationDto> { new("MDC_HDIALY_UF_RATE", "500", "ml/h") };
        DetectedIssue? result = _sut.Evaluate("session-1", "MRN001", observations);
        result.ShouldBeNull();
    }

    [Fact]
    public void Evaluate_WhenSystolicAboveThreshold_ReturnsNull()
    {
        var observations = new List<ObservationDto>
        {
            new("MDC_PRESS_BLD_SYS", "120", "mmHg"),
            new("MDC_PRESS_BLD_DIA", "80", "mmHg")
        };
        DetectedIssue? result = _sut.Evaluate("session-1", "MRN001", observations);
        result.ShouldBeNull();
    }

    [Fact]
    public void Evaluate_WhenSystolicBelow90_ReturnsDetectedIssue()
    {
        var observations = new List<ObservationDto> { new("MDC_PRESS_BLD_SYS", "85", "mmHg") };
        DetectedIssue? result = _sut.Evaluate("session-1", "MRN001", observations);
        result.ShouldNotBeNull();
        result.Id.ShouldBe("cds-hypotension-session-1");
        result.Evidence.ShouldNotBeEmpty();
    }

    [Fact]
    public void Evaluate_WhenDiastolicBelow60_ReturnsDetectedIssue()
    {
        var observations = new List<ObservationDto> { new("MDC_PRESS_BLD_DIA", "55", "mmHg") };
        DetectedIssue? result = _sut.Evaluate("session-1", "MRN001", observations);
        result.ShouldNotBeNull();
        result.Evidence.ShouldNotBeEmpty();
    }
}
