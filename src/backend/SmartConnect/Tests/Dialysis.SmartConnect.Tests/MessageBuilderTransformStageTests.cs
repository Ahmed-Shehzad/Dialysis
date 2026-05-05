using System.Text;
using Dialysis.SmartConnect.ExtendedPlugins;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class MessageBuilderTransformStageTests
{
    private readonly MessageBuilderTransformStage _stage = new();

    [Fact]
    public async Task Prefix_suffix_wraps_payload()
    {
        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            CorrelationId = "c",
            Payload = "mid"u8.ToArray(),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        }.WithMetadata("smartconnect.transform.parameters", """{"prefix":"A","suffix":"Z"}""");

        var r = await _stage.TransformAsync(msg, CancellationToken.None);
        Assert.Equal("AmidZ", Encoding.UTF8.GetString(r.Payload.Span));
    }
}
