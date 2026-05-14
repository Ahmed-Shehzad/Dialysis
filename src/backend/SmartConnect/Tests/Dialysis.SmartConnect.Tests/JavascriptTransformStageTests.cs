using System.Text;
using Dialysis.SmartConnect.ExtendedPlugins;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class JavascriptTransformStageTests
{
    [Fact]
    public async Task Transform_Appends_Via_Script_Async()
    {
        var stage = new JavascriptTransformStage();
        var msg = new IntegrationMessage
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "c1",
            Payload = "ab"u8.ToArray(),
            PayloadFormat = PayloadFormat.Utf8Text,
            Metadata = System.Collections.Immutable.ImmutableDictionary<string, string>.Empty.Add(
                JavascriptTransformStage.ParametersMetadataKey,
                """{"script":"payloadText + 'cd'"}"""),
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await stage.TransformAsync(msg, CancellationToken.None);
        Assert.Equal("abcd", Encoding.UTF8.GetString(result.Payload.Span));
    }
}
