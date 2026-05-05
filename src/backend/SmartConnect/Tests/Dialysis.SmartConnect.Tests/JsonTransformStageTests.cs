using System.Text;
using Dialysis.SmartConnect.Transforms;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class JsonTransformStageTests
{
    private readonly JsonTransformStage _stage = new();

    [Fact]
    public async Task Expression_mode_extracts_nested_value()
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

        var result = await _stage.TransformAsync(msg, CancellationToken.None);
        Assert.Equal("42", Encoding.UTF8.GetString(result.Payload.Span).Trim('"'));
    }

    [Fact]
    public async Task Mappings_mode_builds_object()
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

        var result = await _stage.TransformAsync(msg, CancellationToken.None);
        Assert.Contains("\"out\":\"hello\"", Encoding.UTF8.GetString(result.Payload.Span));
    }
}
