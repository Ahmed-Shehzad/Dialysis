using System.Collections.Immutable;
using System.Text;
using Dialysis.SmartConnect.ExtendedPlugins;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Covers the <c>msg</c> Jint global wired in <see cref="JavascriptTransformStage"/>: scripts that
/// dispatch on HL7 v2 payloads can call <c>msg.GetValue(...)</c> / <c>msg.GetRepeatCount(...)</c> /
/// <c>msg.ToJson()</c> directly without parsing the payload by hand.
/// </summary>
public sealed class JavascriptTransformStageHl7Tests
{
    [Fact]
    public async Task Script_Reads_Pid3_Via_Msg_Global_When_Payload_Is_Hl7_Async()
    {
        var stage = new JavascriptTransformStage();
        var hl7 =
            "MSH|^~\\&|HIS|HOSPITAL|EMR|CLINIC|20260526120000||ADT^A01|MSG-1|P|2.5\r" +
            "PID|||MRN-12345^^^HOSPITAL^MR||DOE^JOHN";

        var message = NewMessage(hl7, """{"script":"msg.GetValue('PID.3.1')"}""");
        var result = await stage.TransformAsync(message, CancellationToken.None);

        Assert.Equal("MRN-12345", Encoding.UTF8.GetString(result.Payload.Span));
    }

    [Fact]
    public async Task Script_Reads_Msh9_Trigger_Via_Msg_Global_Async()
    {
        var stage = new JavascriptTransformStage();
        var hl7 = "MSH|^~\\&|LAB|HOSPITAL|EMR|CLINIC|20260526120000||ORU^R01|MSG-2|P|2.5\r" +
                  "PID|||MRN-9";

        var message = NewMessage(hl7, """{"script":"msg.GetValue('MSH.9.1') + '|' + msg.GetValue('MSH.9.2')"}""");
        var result = await stage.TransformAsync(message, CancellationToken.None);

        Assert.Equal("ORU|R01", Encoding.UTF8.GetString(result.Payload.Span));
    }

    [Fact]
    public async Task Msg_Global_Is_Undefined_When_Payload_Is_Not_Hl7_Async()
    {
        var stage = new JavascriptTransformStage();
        var message = NewMessage(
            "not an hl7 message",
            """{"script":"typeof msg === 'undefined' ? 'unset' : 'set'"}""");

        var result = await stage.TransformAsync(message, CancellationToken.None);

        Assert.Equal("unset", Encoding.UTF8.GetString(result.Payload.Span));
    }

    [Fact]
    public async Task Get_Repeat_Count_Available_From_Script_Async()
    {
        var stage = new JavascriptTransformStage();
        var hl7 =
            "MSH|^~\\&|HIS|HOSPITAL|EMR|CLINIC|20260526120000||ADT^A01|MSG-3|P|2.5\r" +
            "PID|||MRN-1^^^H^MR~SSN-1^^^USA^SS~MRN-2^^^H^MR";

        var message = NewMessage(hl7, """{"script":"msg.GetRepeatCount('PID.3').toString()"}""");
        var result = await stage.TransformAsync(message, CancellationToken.None);

        Assert.Equal("3", Encoding.UTF8.GetString(result.Payload.Span));
    }

    private static IntegrationMessage NewMessage(string payload, string parametersJson) => new()
    {
        Id = Guid.CreateVersion7(),
        FlowId = Guid.CreateVersion7(),
        CorrelationId = "corr-" + Guid.NewGuid().ToString("N")[..8],
        Payload = Encoding.UTF8.GetBytes(payload),
        PayloadFormat = PayloadFormat.Utf8Text,
        Metadata = ImmutableDictionary<string, string>.Empty.Add(
            JavascriptTransformStage.ParametersMetadataKey,
            parametersJson),
        ReceivedAtUtc = DateTimeOffset.UtcNow,
    };
}
