using Dialysis.Prediction.Handlers;
using Dialysis.TestUtilities;
using Shouldly;
using Verifier;
using Xunit;

namespace Dialysis.Prediction.UnitTests;

public sealed class ObservationCreatedValidatorTests
{
    [Fact]
    public void Valid_event_passes()
    {
        var validator = new ObservationCreatedValidator();
        var evt = BogusFakers.ObservationCreatedFaker().Generate();
        var result = validator.Validate(evt);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_observationId_fails()
    {
        var validator = new ObservationCreatedValidator();
        var evt = BogusFakers.ObservationCreatedFaker().Generate() with { ObservationId = default };
        var result = validator.Validate(evt);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_patientId_fails()
    {
        var validator = new ObservationCreatedValidator();
        var evt = BogusFakers.ObservationCreatedFaker().Generate() with { PatientId = default };
        var result = validator.Validate(evt);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_encounterId_fails()
    {
        var validator = new ObservationCreatedValidator();
        var evt = BogusFakers.ObservationCreatedFaker().Generate() with { EncounterId = default };
        var result = validator.Validate(evt);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_code_fails()
    {
        var validator = new ObservationCreatedValidator();
        var evt = BogusFakers.ObservationCreatedFaker().Generate() with { Code = "" };
        var result = validator.Validate(evt);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_value_fails()
    {
        var validator = new ObservationCreatedValidator();
        var evt = BogusFakers.ObservationCreatedFaker().Generate() with { Value = "" };
        var result = validator.Validate(evt);
        result.IsValid.ShouldBeFalse();
    }
}
