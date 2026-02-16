using Dialysis.Alerting.Features.ProcessAlerts;
using Dialysis.TestUtilities;
using Shouldly;
using Verifier;
using Xunit;

namespace Dialysis.Alerting.UnitTests;

public sealed class CreateAlertValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var validator = new CreateAlertValidator();
        var cmd = BogusFakers.CreateAlertCommandFaker().Generate();
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_patientId_fails()
    {
        var validator = new CreateAlertValidator();
        var cmd = BogusFakers.CreateAlertCommandFaker().Generate() with { PatientId = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_encounterId_fails()
    {
        var validator = new CreateAlertValidator();
        var cmd = BogusFakers.CreateAlertCommandFaker().Generate() with { EncounterId = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_code_fails()
    {
        var validator = new CreateAlertValidator();
        var cmd = BogusFakers.CreateAlertCommandFaker().Generate() with { Code = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_severity_fails()
    {
        var validator = new CreateAlertValidator();
        var cmd = BogusFakers.CreateAlertCommandFaker().Generate() with { Severity = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }
}
