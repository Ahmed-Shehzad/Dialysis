using Dialysis.IdentityAdmission.Features.SessionScheduling;
using Dialysis.TestUtilities;
using Shouldly;
using Verifier;
using Xunit;

namespace Dialysis.IdentityAdmission.UnitTests;

public sealed class CreateSessionValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var validator = new CreateSessionValidator();
        var cmd = BogusFakers.CreateSessionCommandFaker().Generate();
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_patientId_fails()
    {
        var validator = new CreateSessionValidator();
        var cmd = BogusFakers.CreateSessionCommandFaker().Generate() with { PatientId = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_deviceId_fails()
    {
        var validator = new CreateSessionValidator();
        var cmd = BogusFakers.CreateSessionCommandFaker().Generate() with { DeviceId = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }
}
