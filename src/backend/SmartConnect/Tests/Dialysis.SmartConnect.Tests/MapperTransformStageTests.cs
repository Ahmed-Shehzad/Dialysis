using System.Text;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.Transforms;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class MapperTransformStageTests
{
    private readonly MapperTransformStage _stage = new(new JsonTransformStage());

    [Fact]
    public async Task Uses_Json_Mapper_Parameters_Async()
    {
        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            CorrelationId = "c",
            Payload = """{"a":1}"""u8.ToArray(),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        }.WithMetadata("smartconnect.transform.parameters", """{"expression":"$.a"}""");

        var r = await _stage.TransformAsync(msg, CancellationToken.None);
        Assert.Equal("1", Encoding.UTF8.GetString(r.Payload.Span).Trim());
    }
}
