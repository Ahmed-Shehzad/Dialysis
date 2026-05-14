using System.Text;
using Dialysis.SmartConnect.Transforms;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class JsonTransformStageTests
{
    private readonly JsonTransformStage _Stage = new();

    [Fact]
    public async Task Expression_Mode_Extracts_Nested_Value_Async()
    {
        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            CorrelationId = "c1",
            Payload = """{"a":{"b":42}}"""u8.ToArray(),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        }.WithMetadata("smartconnect.transform.parameters", """{"expression":"$.a.b"}""");

        var result = await _Stage.TransformAsync(msg, CancellationToken.None);
        Assert.Equal("42", Encoding.UTF8.GetString(result.Payload.Span).Trim('"'));
    }

    [Fact]
    public async Task Mappings_Mode_Builds_Object_Async()
    {
        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            CorrelationId = "c2",
            Payload = """{"x":{"y":"hello"}}"""u8.ToArray(),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        }.WithMetadata(
            "smartconnect.transform.parameters",
            """{"mappings":{"out":"$.x.y"}}""");

        var result = await _Stage.TransformAsync(msg, CancellationToken.None);
        Assert.Contains("\"out\":\"hello\"", Encoding.UTF8.GetString(result.Payload.Span));
    }
}
