using Dialysis.Alerting.Features.ProcessAlerts;
using Dialysis.TestUtilities;
using Shouldly;
using Verifier;
using Xunit;

namespace Dialysis.Alerting.UnitTests;

public sealed class AcknowledgeAlertValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var validator = new AcknowledgeAlertValidator();
        var cmd = BogusFakers.AcknowledgeAlertCommandFaker().Generate();
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_alertId_fails()
    {
        var validator = new AcknowledgeAlertValidator();
        var cmd = new AcknowledgeAlertCommand { AlertId = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }
}
