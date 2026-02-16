using Dialysis.IdentityAdmission.Features.PatientAdmission;
using Dialysis.TestUtilities;
using Shouldly;
using Verifier;
using Xunit;

namespace Dialysis.IdentityAdmission.UnitTests;

public sealed class AdmitPatientValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var validator = new AdmitPatientValidator();
        var cmd = BogusFakers.AdmitPatientCommandFaker().Generate();
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_mrn_fails()
    {
        var validator = new AdmitPatientValidator();
        var cmd = BogusFakers.AdmitPatientCommandFaker().Generate() with { Mrn = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_familyName_fails()
    {
        var validator = new AdmitPatientValidator();
        var cmd = BogusFakers.AdmitPatientCommandFaker().Generate() with { FamilyName = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Null_givenName_passes()
    {
        var validator = new AdmitPatientValidator();
        var cmd = BogusFakers.AdmitPatientCommandFaker().Generate() with { GivenName = (string?)null };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeTrue();
    }
}
