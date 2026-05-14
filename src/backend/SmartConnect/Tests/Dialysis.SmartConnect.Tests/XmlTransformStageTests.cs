using System.Text;
using Dialysis.SmartConnect.Transforms;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class XmlTransformStageTests
{
    private readonly XmlTransformStage _stage = new();

    [Fact]
    public async Task Xpath_Extracts_Element_Text_Async()
    {
        var xml = "<root><code>AE</code></root>";
        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes(xml),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        }.WithMetadata("smartconnect.transform.parameters", """{"xpath":"/root/code/text()"}""");

        var result = await _stage.TransformAsync(msg, CancellationToken.None);
        Assert.Equal("AE", Encoding.UTF8.GetString(result.Payload.Span));
    }
}
