using Dialysis.AuditConsent.Features.Audit;
using Dialysis.TestUtilities;
using Shouldly;
using Verifier;
using Xunit;

namespace Dialysis.AuditConsent.UnitTests;

public sealed class RecordAuditValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var validator = new RecordAuditValidator();
        var cmd = BogusFakers.RecordAuditCommandFaker().Generate();
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_resourceType_fails()
    {
        var validator = new RecordAuditValidator();
        var cmd = BogusFakers.RecordAuditCommandFaker().Generate() with { ResourceType = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_resourceId_fails()
    {
        var validator = new RecordAuditValidator();
        var cmd = BogusFakers.RecordAuditCommandFaker().Generate() with { ResourceId = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Empty_action_fails()
    {
        var validator = new RecordAuditValidator();
        var cmd = BogusFakers.RecordAuditCommandFaker().Generate() with { Action = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
    }
}
