using Dialysis.HisIntegration.Features.AdtSync;
using Shouldly;
using Verifier;
using Xunit;

namespace Dialysis.HisIntegration.UnitTests;

public sealed class AdtIngestValidatorTests
{
    [Fact]
    public async Task Valid_command_passes()
    {
        var validator = new AdtIngestValidator();
        var cmd = new AdtIngestCommand { MessageType = "ADT-A01", RawMessage = "MSH|^~\\&|HIS|HOSP|||20240115120000||ADT^A01|MSG001|P|2.5" };
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_messageType_fails(string messageType)
    {
        var validator = new AdtIngestValidator();
        var cmd = new AdtIngestCommand { MessageType = messageType, RawMessage = "MSH|^~\\&|HIS" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "MessageType");
    }

    [Fact]
    public void Empty_rawMessage_fails()
    {
        var validator = new AdtIngestValidator();
        var cmd = new AdtIngestCommand { MessageType = "ADT-A01", RawMessage = "" };
        var result = validator.Validate(cmd);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "RawMessage");
    }
}
