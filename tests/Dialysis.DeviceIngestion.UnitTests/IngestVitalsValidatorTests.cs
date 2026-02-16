using Dialysis.DeviceIngestion.Features.IngestVitals;
using Dialysis.TestUtilities;
using Shouldly;
using Xunit;

namespace Dialysis.DeviceIngestion.UnitTests;

public sealed class IngestVitalsValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var validator = new IngestVitalsValidator();
        var cmd = BogusFakers.IngestVitalsCommandFaker().Generate();
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Empty_patientId_fails(string? patientId)
    {
        var validator = new IngestVitalsValidator();
        var cmd = BogusFakers.IngestVitalsCommandFaker().Generate() with { PatientId = patientId ?? "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "PatientId");
    }

    [Fact]
    public void Empty_encounterId_fails()
    {
        var validator = new IngestVitalsValidator();
        var cmd = BogusFakers.IngestVitalsCommandFaker().Generate() with { EncounterId = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_deviceId_fails()
    {
        var validator = new IngestVitalsValidator();
        var cmd = BogusFakers.IngestVitalsCommandFaker().Generate() with { DeviceId = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Null_readings_fails()
    {
        var validator = new IngestVitalsValidator();
        var cmd = BogusFakers.IngestVitalsCommandFaker().Generate() with { Readings = null! };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_readings_fails()
    {
        var validator = new IngestVitalsValidator();
        var cmd = BogusFakers.IngestVitalsCommandFaker().Generate() with { Readings = [] };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }
}
